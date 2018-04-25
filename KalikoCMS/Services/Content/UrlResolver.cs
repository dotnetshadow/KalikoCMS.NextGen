﻿namespace KalikoCMS.Services.Content {
    using System.Linq;
    using ContentProviders;
    using Core;
    using Infrastructure;
    using Interfaces;

    public class UrlResolver : IUrlResolver {
        private readonly IContentIndexService _contentIndexService;

        public UrlResolver(IContentIndexService contentIndexService) {
            _contentIndexService = contentIndexService;
        }

        public Content GetContent(string path) {
            return GetContent(path, false);
        }

        public Content GetContent(string path, bool returnPartialMatches) {
            // TODO: Add persinstance and support multiple sites
            var site = _contentIndexService.GetRootNodes(SiteContentProvider.SiteContentTypeId).FirstOrDefault();
            if (site == null) {
                return null;
            }

            if (path == "/") {
                var startContent = site.Children.FirstOrDefault();
                if (startContent == null) {
                    return null;
                }

                return _contentIndexService.GetContentFromNode(startContent);
            }

            // TODO: Add domain and language resolver

            var segments = path.Trim('/').Split('/');
            var node = site;
            foreach (var segment in segments) {
                var parent = node;
                node = node.Children.FirstOrDefault(x => x.Languages.Any(l => l.UrlSegment == segment));
                if (node != null) {
                    continue;
                }

                if (!returnPartialMatches) {
                    return null;
                }

                if (parent == site) {
                    return null;
                }

                return _contentIndexService.GetContentFromNode(parent);
            }

            // TODO: Access rights check
            return _contentIndexService.GetContentFromNode(node);
        }
    }
}