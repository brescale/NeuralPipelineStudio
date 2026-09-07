namespace NeuralPipelineStudio.Models
{
    public class WikiArticle
    {
        public string Title { get; set; } = string.Empty;
        public string Icon { get; set; } = "📖";
        public string Category { get; set; } = "Architettura";
        public string Summary { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string MenuDisplay => $"{Icon}  {Title}";
    }
}