using Microsoft.AspNetCore.Html;

namespace HomeownersSubdivision.ViewModels
{
    public class LuxuryCardViewModel
    {
        public string Title { get; set; }
        public string Icon { get; set; }
        public string Description { get; set; }
        public IHtmlContent Content { get; set; }
        
        public string ButtonText { get; set; }
        public string ButtonUrl { get; set; }
        public string ButtonIcon { get; set; }
        
        public string DelayClass { get; set; }
    }
} 