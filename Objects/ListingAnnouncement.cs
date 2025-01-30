public class AnnouncementResponse
{
    public Data Data { get; set; } = new Data();
}

public class Data
{
    public List<Catalog> Catalogs { get; set; } = new List<Catalog>();
}

public class Catalog
{
    public int CatalogId { get; set; }
    public int? ParentCatalogId { get; set; }
    public string Icon { get; set; } = string.Empty;
    public string CatalogName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int CatalogType { get; set; }
    public int Total { get; set; }
    public List<Article> Articles { get; set; } = new List<Article>();
    public List<Catalog> Catalogs { get; set; } = new List<Catalog>();
}

public class Article
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public int Type { get; set; }
    public long ReleaseDate { get; set; }
}