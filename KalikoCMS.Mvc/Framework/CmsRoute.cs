﻿namespace KalikoCMS.Mvc.Framework {
#if NETCORE
    using System;
    using System.Globalization;
    using System.Threading;
    using System.Threading.Tasks;
    using Configuration.Interfaces;
    using Core;
    using KalikoCMS.Extensions;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.Infrastructure;
    using Microsoft.AspNetCore.Mvc.Internal;
    using Microsoft.AspNetCore.Routing;
    using Services.Content.Interfaces;
#else
    using System.Web.Routing;
    using Interfaces;
#endif

#if NETCORE
    public class CmsRoute : IRouter, INamedRouter {
        private readonly IActionSelector _actionSelector;
        private readonly IActionInvokerFactory _actionInvokerFactory;
        private readonly IActionContextAccessor _actionContextAccessor;
        private readonly IUrlResolver _urlResolver;
        private readonly IContentLoader _contentLoader;
        private readonly ICmsConfiguration _configuration;
#else
    public class CmsRoute : Route {
#endif

        #region Constructors

#if NETCORE
        public CmsRoute(IActionSelector actionSelector, IActionInvokerFactory actionInvokerFactory, IActionContextAccessor actionContextAccessor, IUrlResolver urlResolver, IContentLoader contentLoader, ICmsConfiguration configuration) {
            _actionSelector = actionSelector;
            _actionInvokerFactory = actionInvokerFactory;
            _actionContextAccessor = actionContextAccessor;
            _urlResolver = urlResolver;
            _contentLoader = contentLoader;
            _configuration = configuration;
        }
#else
        public CmsRoute(string url, IRouteHandler routeHandler) : base(url, routeHandler) { }

        public CmsRoute(string url, RouteValueDictionary defaults, IRouteHandler routeHandler) : base(url, defaults, routeHandler) { }

        public CmsRoute(string url, RouteValueDictionary defaults, RouteValueDictionary constraints, IRouteHandler routeHandler) : base(url, defaults, constraints, routeHandler) { }

        public CmsRoute(string url, RouteValueDictionary defaults, RouteValueDictionary constraints, RouteValueDictionary dataTokens, IRouteHandler routeHandler) : base(url, defaults, constraints, dataTokens, routeHandler) { }
#endif

        #endregion

#if NETCORE
        public string Name => "CmsRoute";

        public Task RouteAsync(RouteContext context) {
            if (context == null) {
                throw new ArgumentNullException(nameof(context));
            }

            if (!Bootstrapper.Initialize()) {
                //context.HttpContext.Response.RenderMessage("System is starting up..", "Please check back in a few seconds.", 503);
                //context.HttpContext.Response.ContentType = "text/text";
                //context.HttpContext.Response.WriteAsync("System is starting up");
                context.Handler = c => {
                    c.Response.ContentType = "text/text";
                    c.Response.WriteAsync("System is starting up... Please check back in a few seconds");
                    return Task.CompletedTask;
                };

                return Task.CompletedTask;
            }

            var path = context.HttpContext.Request.Path;
            var content = _urlResolver.GetContent(path, true);

            if (content == null || !content.IsAvailable()) {
                return Task.CompletedTask;
            }

            var action = "Index";
            var contentUrlLength = content.ContentUrl.Length + (_configuration.SkipEndingSlash ? 1 : 0);
            if (path.Value.Length > contentUrlLength) {
                var remains = path.Value.Substring(contentUrlLength).Trim('/');
                if (remains.IndexOf('/') > 0) {
                    // Not an action
                    return Task.CompletedTask;
                }
                else {
                    action = remains;
                }
            }

            var site = _contentLoader.GetClosest<CmsSite>(content.ContentReference);

            // TODO: Refactor
            var controllerType = RequestModule.GetControllerType(content);
            var controllerTypeName = controllerType.Name;
            if (controllerTypeName.EndsWith("Controller")) {
                controllerTypeName = controllerTypeName.Substring(0, controllerTypeName.Length - 10);
            }

            context.RouteData.Values["controller"] = controllerTypeName;
            context.RouteData.Values["action"] = action;
            context.RouteData.Values["currentPage"] = content;
            context.RouteData.Values["currentSite"] = site;

            context.HttpContext.Items["cmsCurrentPage"] = content;
            context.HttpContext.Items["cmsCurrentSite"] = site;
            context.HttpContext.Items["cmsLanguageId"] = content.LanguageId;

            // TODO: Lift from system
            //Get the culture info of the language code
            var culture = CultureInfo.GetCultureInfo("en");
            Thread.CurrentThread.CurrentCulture = culture;
            Thread.CurrentThread.CurrentUICulture = culture;

            var candidates = _actionSelector.SelectCandidates(context);
            if (candidates == null || candidates.Count == 0) {
                //_logger.NoActionsMatched(context.RouteData.Values);
                return Task.CompletedTask;
            }

            var actionDescriptor = _actionSelector.SelectBestCandidate(context, candidates);
            if (actionDescriptor == null) {
                //_logger.NoActionsMatched(context.RouteData.Values);
                return Task.CompletedTask;
            }

            context.Handler = c => {
                var routeData = c.GetRouteData();

                var actionContext = new ActionContext(context.HttpContext, routeData, actionDescriptor);
                if (_actionContextAccessor != null) {
                    _actionContextAccessor.ActionContext = actionContext;
                }

                var invoker = _actionInvokerFactory.CreateInvoker(actionContext);
                if (invoker == null) {
                    throw new InvalidOperationException("Resources.FormatActionInvokerFactory_CouldNotCreateInvoker(actionDescriptor.DisplayName)");
                }

                return invoker.InvokeAsync();
            };

            return Task.CompletedTask;
        }

        public VirtualPathData GetVirtualPath(VirtualPathContext context) {
            if(!context.AmbientValues.ContainsKey("currentPage")) {
                return null;
            }

            var currentController = context.AmbientValues.ContainsKey("controller") ? (string)context.AmbientValues["controller"] : "";
            var requestedController = context.Values.ContainsKey("controller") ? (string)context.Values["controller"] : "";
            if (currentController != requestedController) {
                return null;
            }

            var currentPage = context.AmbientValues["currentPage"] as CmsPage;
            var action = context.Values.ContainsKey("action") ? context.Values["action"] : null;
            
            return new VirtualPathData(this, $"{currentPage?.ContentUrl}{action}", context.Values);
        }
#else
        public override VirtualPathData GetVirtualPath(RequestContext requestContext, RouteValueDictionary values) {
            object controller = null;

            if (requestContext.HttpContext.Items.Contains("cmsRouting") && values.ContainsKey("controller")) {
                controller = values["controller"];
                values.Remove("controller");
            }

            var virtualPath = base.GetVirtualPath(requestContext, values);

            if (controller != null) {
                values.Add("controller", controller);
            }

            return virtualPath;
        }
#endif
    }
}