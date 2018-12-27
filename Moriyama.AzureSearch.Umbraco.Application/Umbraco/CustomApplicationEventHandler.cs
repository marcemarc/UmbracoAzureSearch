using AutoMapper;
using Microsoft.Azure.Search.Models;
using Moriyama.AzureSearch.Umbraco.Application.Models;
using Moriyama.AzureSearch.Umbraco.Application.SearchableTrees;
using System.Configuration;
using System.Web;
using Umbraco.Core;
using Umbraco.Core.Events;
using Umbraco.Core.Models;
using Umbraco.Core.Publishing;
using Umbraco.Core.Services;
using Umbraco.Web.Search;
using Umbraco.Web.Trees;

namespace Moriyama.AzureSearch.Umbraco.Application.Umbraco
{
    public class CustomApplicationEventHandler : ApplicationEventHandler
    {
        protected override void ApplicationStarted(UmbracoApplicationBase umbracoApplication, ApplicationContext applicationContext)
        {
            Mapper.CreateMap<Field, SearchField>().ForMember(dest => dest.Type,
               opts => opts.MapFrom(
                   src => src.Type.ToString()
              ));

            Mapper.CreateMap<Index, SearchIndex>();
            Mapper.CreateMap<ScoringProfile, AzureItemBase>();
            Mapper.CreateMap<Suggester, AzureItemBase>();
            Mapper.CreateMap<Field, AzureItemBase>();

            var appRoot = HttpContext.Current.Server.MapPath("/");
            AzureSearchContext.Instance.SetupSearchClient<AzureSearchClient>(appRoot);
            AzureSearchContext.Instance.SearchIndexClient = new AzureSearchIndexClient(appRoot);

            ContentService.Saved += ContentServiceSaved;
            ContentService.Published += ContentServicePublished;
            ContentService.Trashed += ContentServiceTrashed;
            ContentService.Deleted += ContentServiceDeleted;
            ContentService.EmptiedRecycleBin += ContentServiceEmptiedRecycleBin;

            MediaService.Saved += MediaServiceSaved;
            MediaService.Trashed += MediaServiceTrashed;
            MediaService.Deleted += MediaServiceDeleted;

            MemberService.Saved += MemberServiceSaved;
            MemberService.Deleted += MemberServiceDeleted;
        }
        protected override void ApplicationStarting(UmbracoApplicationBase umbracoApplication, ApplicationContext applicationContext)
        {
            //ISearchableTree - any class implementing ISearchableTree is automatically picked up by Umbraco and configured to work with the specified TreeAlias
            //therefore depending on the appsetting: UmbracoAzureSearch.ReplaceBackofficeSearch we need to remove the types we don't want.
            bool replaceBackofficeSearch = false;

            if (bool.TryParse(ConfigurationManager.AppSettings["UmbracoAzureSearch.ReplaceBackofficeSearch"], out replaceBackofficeSearch))
            {
                //replace core ISearchableTree instances for Content, Members and Media
                SearchableTreeResolver.Current.RemoveType<ContentTreeController>();
                SearchableTreeResolver.Current.RemoveType<MemberTreeController>();
                SearchableTreeResolver.Current.RemoveType<MediaTreeController>();
            }
            else
            {
                //remove the Umbraco Azure Search SearchableTree implementations
                SearchableTreeResolver.Current.RemoveType<AzureSearchContentSearchableTree>();
                SearchableTreeResolver.Current.RemoveType<AzureSearchMemberSearchableTree>();
                SearchableTreeResolver.Current.RemoveType<AzureSearchMediaSearchableTree>();
            }

        }
        private void ContentServiceEmptiedRecycleBin(IContentService sender, RecycleBinEventArgs e)
        {
            var azureSearchServiceClient = AzureSearchContext.Instance.SearchIndexClient;

            foreach (var id in e.Ids)
            {
                azureSearchServiceClient.Delete(id);
            }
        }

        private void MediaServiceDeleted(IMediaService sender, DeleteEventArgs<IMedia> e)
        {
            var azureSearchServiceClient = AzureSearchContext.Instance.SearchIndexClient;

            foreach (var entity in e.DeletedEntities)
            {
                azureSearchServiceClient.Delete(entity.Id);
            }
        }

        private void ContentServiceDeleted(IContentService sender, DeleteEventArgs<IContent> e)
        {
            var azureSearchServiceClient = AzureSearchContext.Instance.SearchIndexClient;

            foreach (var entity in e.DeletedEntities)
            {
                azureSearchServiceClient.Delete(entity.Id);
            }
        }

        private void ContentServiceSaved(IContentService sender, SaveEventArgs<IContent> e)
        {
            var azureSearchServiceClient = AzureSearchContext.Instance.SearchIndexClient;

            foreach (var entity in e.SavedEntities)
            {
                azureSearchServiceClient.ReIndexContent(entity);
            }
        }

        private void MemberServiceDeleted(IMemberService sender, DeleteEventArgs<IMember> e)
        {
            var azureSearchServiceClient = AzureSearchContext.Instance.SearchIndexClient;

            foreach (var entity in e.DeletedEntities)
            {
                azureSearchServiceClient.Delete(entity.Id);
            }
        }

        private void ContentServiceTrashed(IContentService sender, MoveEventArgs<IContent> e)
        {
            var azureSearchServiceClient = AzureSearchContext.Instance.SearchIndexClient;
            foreach (var item in e.MoveInfoCollection)
            {
                azureSearchServiceClient.ReIndexContent(item.Entity);
            }
        }

        private void MediaServiceTrashed(IMediaService sender, MoveEventArgs<IMedia> e)
        {
            var azureSearchServiceClient = AzureSearchContext.Instance.SearchIndexClient;
            foreach (var item in e.MoveInfoCollection)
            {
                azureSearchServiceClient.ReIndexContent(item.Entity);
            }
        }

        private void MemberServiceSaved(IMemberService sender, SaveEventArgs<IMember> e)
        {
            var azureSearchServiceClient = AzureSearchContext.Instance.SearchIndexClient;
            foreach (var entity in e.SavedEntities)
            {
                azureSearchServiceClient.ReIndexMember(entity);
            }
        }

        private void MediaServiceSaved(IMediaService sender, SaveEventArgs<IMedia> e)
        {
            var azureSearchServiceClient = AzureSearchContext.Instance.SearchIndexClient;
            foreach (var entity in e.SavedEntities)
            {
                azureSearchServiceClient.ReIndexContent(entity);
            }
        }

        private void ContentServicePublished(IPublishingStrategy sender, PublishEventArgs<IContent> e)
        {
            var azureSearchServiceClient = AzureSearchContext.Instance.SearchIndexClient;
            foreach (var entity in e.PublishedEntities)
            {
                azureSearchServiceClient.ReIndexContent(entity);
            }
        }
    }
}