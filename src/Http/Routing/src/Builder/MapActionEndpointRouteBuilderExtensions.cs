// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Internal;

namespace Microsoft.AspNetCore.Builder
{
    /// <summary>
    /// Provides extension methods for <see cref="IEndpointRouteBuilder"/> to define HTTP API endpoints.
    /// </summary>
    public static class MapActionEndpointRouteBuilderExtensions
    {
        /// <summary>
        /// Adds a <see cref="RouteEndpoint"/> to the <see cref="IEndpointRouteBuilder"/> that matches the pattern specified via attributes.
        /// </summary>
        /// <param name="endpoints">The <see cref="IEndpointRouteBuilder"/> to add the route to.</param>
        /// <param name="action">The delegate executed when the endpoint is matched.</param>
        /// <returns>An <see cref="IEndpointConventionBuilder"/> that can be used to further customize the endpoint.</returns>
        public static MapActionEndpointConventionBuilder MapAction(
            this IEndpointRouteBuilder endpoints,
            Delegate action)
        {
            if (endpoints is null)
            {
                throw new ArgumentNullException(nameof(endpoints));
            }

            if (action is null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            var requestDelegate = MapActionExpressionTreeBuilder.BuildRequestDelegate(action);

            var routeAttributes = action.Method.GetCustomAttributes().OfType<IRoutePatternMetadata>();
            var conventionBuilders = new List<IEndpointConventionBuilder>();

            const int defaultOrder = 0;

            foreach (var routeAttribute in routeAttributes)
            {
                if (routeAttribute.RoutePattern is not string pattern)
                {
                    continue;
                }

                var routeName = (routeAttribute as IRouteNameMetadata)?.RouteName;
                var routeOrder = (routeAttribute as IRouteOrderMetadata)?.RouteOrder;

                var conventionBuilder = endpoints.Map(pattern, requestDelegate);

                conventionBuilder.Add(endpointBuilder =>
                {
                    foreach (var attribute in action.Method.GetCustomAttributes())
                    {
                        endpointBuilder.Metadata.Add(attribute);
                    }

                    endpointBuilder.DisplayName = routeName ?? pattern;

                    ((RouteEndpointBuilder)endpointBuilder).Order = routeOrder ?? defaultOrder;
                });

                conventionBuilders.Add(conventionBuilder);
            }

            if (conventionBuilders.Count == 0)
            {
                throw new InvalidOperationException("Action must have a pattern. Is it missing a Route attribute?");
            }

            return new MapActionEndpointConventionBuilder(conventionBuilders);
        }
    }
}
