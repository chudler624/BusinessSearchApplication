namespace BusinessSearch.Models.ViewModels
{
    public class TemplatesScriptsViewModel
    {
        public List<EmailTemplate> EmailTemplates { get; set; } = new List<EmailTemplate>();
        public List<CallScript> CallScripts { get; set; } = new List<CallScript>();
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; } = 1;
        public int PageSize { get; set; } = 25;
        public int TotalItems { get; set; }
        public string? SearchTerm { get; set; }
        public string? CategoryFilter { get; set; }
        public string? TypeFilter { get; set; }
        public string ActiveTab { get; set; } = "email-templates";
    }

    public class CreateEmailTemplateViewModel
    {
        public EmailTemplate Template { get; set; } = new EmailTemplate();
        public List<string> AvailableCategories { get; set; } = new List<string>();
    }

    public class EditEmailTemplateViewModel
    {
        public EmailTemplate Template { get; set; } = new EmailTemplate();
        public List<string> AvailableCategories { get; set; } = new List<string>();
    }

    public class CreateCallScriptViewModel
    {
        public CallScript Script { get; set; } = new CallScript();
        public List<string> AvailableTypes { get; set; } = new List<string>();
        public List<string> AvailableIndustries { get; set; } = new List<string>();
    }

    public class EditCallScriptViewModel
    {
        public CallScript Script { get; set; } = new CallScript();
        public List<string> AvailableTypes { get; set; } = new List<string>();
        public List<string> AvailableIndustries { get; set; } = new List<string>();
    }
}