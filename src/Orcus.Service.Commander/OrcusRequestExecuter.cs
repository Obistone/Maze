﻿using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Orcus.Modules.Api;
using Orcus.Modules.Api.Request;
using Orcus.Modules.Api.Response;
using Orcus.Service.Commander.Routing;

namespace Orcus.Service.Commander
{
    /// <inheritdoc />
    public class OrcusRequestExecuter : IOrcusRequestExecuter
    {
        private readonly ILogger<OrcusRequestExecuter> _logger;
        private readonly IRouteCache _routeCache;
        private readonly IRouteResolver _routeResolver;
        private readonly IServiceProvider _serviceProvider;

        public OrcusRequestExecuter(IRouteResolver routeResolver, IRouteCache routeCache,
            IServiceProvider serviceProvider, ILogger<OrcusRequestExecuter> logger)
        {
            _routeResolver = routeResolver;
            _routeCache = routeCache;
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task<OrcusResponse> Execute(OrcusRequest request)
        {
            var context = new DefaultOrcusContext(request, _serviceProvider);

            _logger.LogDebug($"Resolve Orcus path {context.Request.Path}");
            var result = _routeResolver.Resolve(context);
            if (!result.Success)
            {
                _logger.LogDebug("Path not found");
                return NotFound();
            }

            _logger.LogDebug(
                $"Route resolved (package: {result.RouteDescription.PackageIdentity}). Get cached route info.");
            var route = _routeCache.Routes[result.RouteDescription];
            var actionContext = new DefaultActionContext(context, route, result.Parameters.ToImmutableDictionary());

            _logger.LogDebug($"Invoke method {route.RouteMethod}");

            IActionResult actionResult;
            try
            {
                actionResult = await route.ActionInvoker.Value.Invoke(actionContext);
            }
            catch (Exception e)
            {
                _logger.LogError(e,
                    $"Error occurred when invoking method {route.RouteMethod} of package {result.RouteDescription.PackageIdentity} (path: {context.Request.Path})");
                return Exception(e);
            }

            try
            {
                await actionResult.ExecuteResultAsync(actionContext);
            }
            catch (Exception e)
            {
                _logger.LogError(e,
                    $"Error occurred when executing action result {route.RouteMethod} of package {result.RouteDescription.PackageIdentity} (path: {context.Request.Path})");
                return Exception(e);
            }

            _logger.LogDebug("Request successfully executed.");
            return actionContext.Context.Response;
        }

        private OrcusResponse NotFound()
        {
            throw new NotImplementedException();
        }

        private OrcusResponse Exception(Exception e)
        {
            throw new NotImplementedException();
        }
    }
}