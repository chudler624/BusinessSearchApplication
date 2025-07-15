using System.Text.RegularExpressions;
using BusinessSearch.Models.WebsiteAnalysis;

namespace BusinessSearch.Services.WebsiteOpportunitiesServices.Interfaces
{
    public class ResponsivenessService : IResponsivenessService
    {
        public ResponsivenessResult AnalyzeResponsiveness(string content)
        {
            var result = new ResponsivenessResult { Details = new List<string>(), Score = 0 };

            // Check for viewport meta tag
            if (Regex.IsMatch(content, @"<meta[^>]*name=[""']viewport[""'][^>]*content=[""'][^""']*width\s*=\s*device-width[^""']*[""'][^>]*>"))
            {
                result.Score += 30;
                result.Details.Add("Proper viewport meta tag found (+30 points)");
            }
            else if (Regex.IsMatch(content, @"<meta[^>]*name=[""']viewport[""'][^>]*>"))
            {
                result.Score += 15;
                result.Details.Add("Viewport meta tag found, but may not be properly configured (+15 points)");
            }
            else
            {
                result.Details.Add("No viewport meta tag found");
            }

            // Check for media queries
            var mediaQueryCount = Regex.Matches(content, @"@media\s*\([^)]+\)").Count;
            var mediaQueryScore = Math.Min(mediaQueryCount * 5, 25);
            result.Score += mediaQueryScore;
            result.Details.Add($"Media queries found: {mediaQueryCount} (+{mediaQueryScore} points)");

            // Check for responsive frameworks
            var responsivePatterns = new Dictionary<string, string> {
                { @"class=[""'][^""']*(?:container|row|col(?:-[a-z]+)?-\d+)[^""']*[""']", "Bootstrap-like" },
                { @"class=[""'][^""']*(?:flex|space-[xy]|gap-|grid|md:|lg:)[^""']*[""']", "Tailwind-like" },
                { @"class=[""'][^""']*(?:mui-|mdc-)[^""']*[""']", "Material-UI-like" },
                { @"class=[""'][^""']*(?:ant-)[^""']*[""']", "Ant Design-like" },
                { @"class=[""'][^""']*(?:\b(?:small|medium|large)-\d+)[^""']*[""']", "Foundation-like" },
                { @"class=[""'][^""']*(?:w-\d+\/\d+|flex-\d+)[^""']*[""']", "Custom responsive" }
            };

            foreach (var pattern in responsivePatterns)
            {
                if (Regex.IsMatch(content, pattern.Key))
                {
                    result.Score += 15;
                    result.Details.Add($"{pattern.Value} responsive classes detected (+15 points)");
                    break;
                }
            }

            // Check for flexible images
            var flexibleImageCount = Regex.Matches(content, @"<img[^>]*(?:style=[""'][^""']*(?:max-width:\s*100%|width:\s*100%|height:\s*auto)[^""']*[""']|class=[""'][^""']*(?:img-fluid|img-responsive|w-full)[^""']*[""'])").Count;
            var flexibleImageScore = Math.Min(flexibleImageCount * 5, 15);
            result.Score += flexibleImageScore;
            result.Details.Add($"Flexible images found: {flexibleImageCount} (+{flexibleImageScore} points)");

            // Check for modern CSS layout techniques
            if (Regex.IsMatch(content, @"display\s*:\s*(?:flex|grid)"))
            {
                result.Score += 15;
                result.Details.Add("Modern CSS layout techniques (Flexbox or Grid) detected (+15 points)");
            }

            // Set final responsiveness status
            result.IsResponsive = result.Score >= 50;
            result.Details.Add($"Total score: {result.Score}/100");
            result.Details.Add(result.IsResponsive ?
                "The website appears to be mobile responsive." :
                "The website may not be fully mobile responsive. Consider implementing more responsive design techniques.");

            return result;
        }
    }
}