﻿using System;
using System.Collections.Immutable;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Threading.Tasks;
using LiteDB;
using Microsoft.Extensions.Options;
using Nito.AsyncEx;
using Orcus.Utilities;
using RequestTransmitter.Client;
using Tasks.Infrastructure.Client.Data;
using Tasks.Infrastructure.Client.Library;
using Tasks.Infrastructure.Client.Options;
using Tasks.Infrastructure.Client.Rest.V1;
using Tasks.Infrastructure.Client.Utilities;
using Tasks.Infrastructure.Core;
using Tasks.Infrastructure.Management.Data;
using FileMode = System.IO.FileMode;

namespace Tasks.Infrastructure.Client.Storage
{
    public class DatabaseTaskStorage : IDatabaseTaskStorage
    {
        private readonly IFileSystem _fileSystem;
        private readonly string _sessionsDirectory;
        private readonly AsyncReaderWriterLock _readerWriterLock;
        private readonly IRequestTransmitter _requestTransmitter;

        public DatabaseTaskStorage(IFileSystem fileSystem, IRequestTransmitter requestTransmitter, IOptions<TasksOptions> options)
        {
            _fileSystem = fileSystem;
            _requestTransmitter = requestTransmitter;
            _sessionsDirectory = Environment.ExpandEnvironmentVariables(options.Value.SessionsDirectory);
            _readerWriterLock = new AsyncReaderWriterLock();

            var mapper = BsonMapper.Global;
            mapper.Entity<TaskSession>().Id(x => x.TaskSessionId, autoId: false).DbRef(x => x.Executions, nameof(TaskExecution));
            mapper.Entity<TaskExecution>().Id(x => x.TaskExecutionId).DbRef(x => x.CommandResults, nameof(CommandResult));
            mapper.Entity<OrcusTaskStatus>().Id(x => x.IsFinished, autoId: false);
        }

        public async Task<TaskSession> OpenSession(SessionKey sessionKey, OrcusTask orcusTask, string description)
        {
            using (await _readerWriterLock.ReaderLockAsync())
            {
                var file = _fileSystem.FileInfo.FromFileName(GetTaskDbFilename(orcusTask));
                if (file.Exists)
                    using (var dbStream = file.OpenRead())
                    using (var db = new LiteDatabase(dbStream))
                    {
                        var collection = db.GetCollection<TaskSession>(nameof(TaskSession));
                        collection.EnsureIndex(x => x.TaskSessionId, true);

                        var taskSession = collection.IncludeAll().FindById(sessionKey.Hash);
                        if (taskSession != null)
                            return taskSession;
                    }
            }

            return new TaskSession
            {
                TaskSessionId = sessionKey.Hash,
                Description = description,
                CreatedOn = DateTimeOffset.UtcNow,
                TaskReference = new TaskReference {TaskId = orcusTask.Id},
                TaskReferenceId = orcusTask.Id,
                Executions = ImmutableList<TaskExecution>.Empty
            };
        }

        public async Task StartExecution(OrcusTask orcusTask, TaskSession taskSession, TaskExecution taskExecution)
        {
            using (await _readerWriterLock.WriterLockAsync())
            {
                var file = _fileSystem.FileInfo.FromFileName(GetTaskDbFilename(orcusTask));
                file.Directory.Create();

                var transmitterQueue = new TaskQueue();
                try
                {
                    using (var dbStream = file.Open(FileMode.OpenOrCreate, FileAccess.ReadWrite))
                    using (var db = new LiteDatabase(dbStream))
                    {
                        var sessions = db.GetCollection<TaskSession>(nameof(TaskSession));
                        sessions.EnsureIndex(x => x.TaskSessionId, unique: true);

                        var taskSessionEntity = sessions.FindById(taskSession.TaskSessionId);
                        if (taskSessionEntity == null)
                        {
                            taskSessionEntity = new TaskSession
                            {
                                TaskSessionId = taskSession.TaskSessionId,
                                TaskReferenceId = orcusTask.Id,
                                Description = taskSession.Description,
                                CreatedOn = DateTimeOffset.UtcNow
                            };
                            sessions.Insert(taskSessionEntity);

                            transmitterQueue.Enqueue(() => _requestTransmitter.Transmit(TaskSessionsResource.CreateSessionRequest(taskSessionEntity))).Forget();
                        }

                        taskExecution.TaskSessionId = taskSessionEntity.TaskSessionId;

                        var executions = db.GetCollection<TaskExecution>(nameof(TaskExecution));
                        taskExecution.TaskExecutionId = executions.Insert(taskExecution);

                        transmitterQueue.Enqueue(() => _requestTransmitter.Transmit(TaskExecutionsResource.CreateExecutionRequest(taskExecution))).Forget();
                    }
                }
                finally 
                {
                    await transmitterQueue.DisposeAsync();
                }
            }
        }

        public async Task MarkTaskFinished(OrcusTask orcusTask)
        {
            using (await _readerWriterLock.WriterLockAsync())
            {
                var file = _fileSystem.FileInfo.FromFileName(GetTaskDbFilename(orcusTask));
                file.Directory.Create();

                using (var dbStream = file.Open(FileMode.OpenOrCreate, FileAccess.ReadWrite))
                using (var db = new LiteDatabase(dbStream))
                {
                    var entities = db.GetCollection<OrcusTaskStatus>(nameof(OrcusTaskStatus));
                    entities.Delete(x => true);

                    entities.Insert(new OrcusTaskStatus {IsFinished = true});
                }
            }
        }

        public async Task<bool> CheckTaskFinished(OrcusTask orcusTask)
        {
            using (await _readerWriterLock.ReaderLockAsync())
            {
                var file = _fileSystem.FileInfo.FromFileName(GetTaskDbFilename(orcusTask));
                if (!file.Exists)
                    return false;

                using (var dbStream = file.Open(FileMode.Open, FileAccess.ReadWrite))
                using (var db = new LiteDatabase(dbStream))
                {
                    var entities = db.GetCollection<OrcusTaskStatus>(nameof(OrcusTaskStatus));
                    var entity = entities.FindAll().FirstOrDefault();

                    return entity?.IsFinished == true;
                }
            }
        }

        public async Task AppendCommandResult(OrcusTask orcusTask, CommandResult commandResult)
        {
            using (await _readerWriterLock.WriterLockAsync())
            {
                var file = _fileSystem.FileInfo.FromFileName(GetTaskDbFilename(orcusTask));
                file.Directory.Create();

                using (var dbStream = file.Open(FileMode.OpenOrCreate, FileAccess.ReadWrite))
                using (var db = new LiteDatabase(dbStream))
                {
                    var results = db.GetCollection<CommandResult>(nameof(CommandResult));
                    results.Insert(commandResult);

                    await _requestTransmitter.Transmit(TaskExecutionsResource.CreateCommandResultRequest(commandResult));
                }
            }
        }

        private string GetTaskDbFilename(OrcusTask orcusTask) => _fileSystem.Path.Combine(_sessionsDirectory, orcusTask.Id.ToString("N"));
    }
}