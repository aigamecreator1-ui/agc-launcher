namespace AGC.Server.Controllers;

public sealed class PublishGameForm
{
    public required string Title { get; set; }
    public required string Description { get; set; }
    public required string Genre { get; set; }
    public string Tags { get; set; } = string.Empty;
    public bool IsPaid { get; set; }
    public decimal PriceUsd { get; set; }
    public required IFormFile Build { get; set; }
    public required IFormFile Thumbnail { get; set; }
}
