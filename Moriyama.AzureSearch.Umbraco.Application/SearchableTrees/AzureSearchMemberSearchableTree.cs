using Moriyama.AzureSearch.Umbraco.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Umbraco.Web.Models.ContentEditing;
using Umbraco.Web.Search;

namespace Moriyama.AzureSearch.Umbraco.Application.SearchableTrees
{
    public class AzureSearchMemberSearchableTree : ISearchableTree
    {
        public string TreeAlias => "member";

        public IEnumerable<SearchResultItem> Search(string query, int pageSize, long pageIndex, out long totalFound, string searchFrom = null)
        {
            IAzureSearchClient client = AzureSearchContext.Instance.GetSearchClient();
            // if the search term contains a space this will be transformed to %20 and no search results returned
            // so lets decode the query term to turn it back into a proper space
            // will this mess up any other Url encoded terms? or fix them too?
            query = HttpUtility.UrlDecode(query);
            //build up query

            //pageIndex is 0 indexed, whereas AzureSearch integration expects actual page number so we always add 1 to the pageIndex
            var resultPage = (int)pageIndex + 1;

            client
                 .Member()
                 .Term(query + "*")
                 .Page(resultPage)
                 .PageSize(pageSize);
            // I believe in the members section searchFrom will be a member type alias
            if (!String.IsNullOrEmpty(searchFrom))
            {
                client.DocumentType(searchFrom);
            }
            var azureSearchResults = client.Results();

            List<SearchResultItem> searchResults = new List<SearchResultItem>();
            totalFound = 0;
            if (azureSearchResults.Content.Any())
            {
                totalFound = azureSearchResults.Content.Count;
                foreach (var azureSearchResult in azureSearchResults.Content)
                {
                    //TODO: add Udi and Icon
                    searchResults.Add(new SearchResultItem() { Id = azureSearchResult.Id, Score = (float)azureSearchResult.Score, Alias = azureSearchResult.ContentTypeAlias, Icon = "icon-document", Name = azureSearchResult.Name, Key = new Guid(azureSearchResult.Key), ParentId = azureSearchResult.ParentId, Path = string.Join(",", azureSearchResult.Path), Trashed = azureSearchResult.Trashed });
                }
            }
            return searchResults;
        }
    }
}
