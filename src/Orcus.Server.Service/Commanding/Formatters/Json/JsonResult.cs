// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Orcus.Modules.Api;
using Orcus.Modules.Api.Response;
using Orcus.Server.Service.Commanding.Formatters.Json.Internal;

namespace Orcus.Server.Service.Commanding.Formatters.Json
{
    /// <summary>
    /// An action result which formats the given object as JSON.
    /// </summary>
    public class JsonResult : ActionResult
    {
        /// <summary>
        /// Creates a new <see cref="JsonResult"/> with the given <paramref name="value"/>.
        /// </summary>
        /// <param name="value">The value to format as JSON.</param>
        public JsonResult(object value)
        {
            Value = value;
        }

        /// <summary>
        /// Creates a new <see cref="JsonResult"/> with the given <paramref name="value"/>.
        /// </summary>
        /// <param name="value">The value to format as JSON.</param>
        /// <param name="serializerSettings">The <see cref="JsonSerializerSettings"/> to be used by
        /// the formatter.</param>
        public JsonResult(object value, JsonSerializerSettings serializerSettings)
        {
            if (serializerSettings == null)
            {
                throw new ArgumentNullException(nameof(serializerSettings));
            }

            Value = value;
            SerializerSettings = serializerSettings;
        }

        /// <summary>
        /// Gets or sets the <see cref="MediaTypeHeaderValue"/> representing the Content-Type header of the response.
        /// </summary>
        public string ContentType { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="JsonSerializerSettings"/>.
        /// </summary>
        public JsonSerializerSettings SerializerSettings { get; set; }

        /// <summary>
        /// Gets or sets the HTTP status code.
        /// </summary>
        public int? StatusCode { get; set; }

        /// <summary>
        /// Gets or sets the value to be formatted.
        /// </summary>
        public object Value { get; set; }

        /// <inheritdoc />
        public override Task ExecuteResultAsync(ActionContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var services = context.Context.RequestServices;
            var executor = services.GetRequiredService<JsonResultExecutor>();
            return executor.ExecuteAsync(context, this);
        }
    }
}
