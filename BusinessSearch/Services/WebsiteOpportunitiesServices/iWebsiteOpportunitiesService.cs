﻿using System.Threading.Tasks;
using BusinessSearch.Models.WebsiteAnalysis;

namespace BusinessSearch.Services.WebsiteOpportunitiesServices
{
    public interface IWebsiteOpportunitiesService
    {
        Task<WebsiteAnalysisResult> AnalyzeWebsite(string url);
    }
}
