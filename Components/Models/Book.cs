using SurrealDb.Net.Models;

namespace Clipser.Components.Models;

public class Book : Record
{
    public List<string> author_alternative_name { get; set; }
    public List<string> author_key { get; set; }
    public List<string> author_name { get; set; }
    public List<string> contributor { get; set; }
    public string cover_edition_key { get; set; }
    public int cover_i { get; set; }
    public string ebook_access { get; set; }
    public int ebook_count_i { get; set; }
    public int edition_count { get; set; }
    public List<string> edition_key { get; set; }
    public int first_publish_year { get; set; }
    public List<string> format { get; set; }
    public bool has_fulltext { get; set; }
    public List<string> ia { get; set; }
    public List<string> ia_collection { get; set; }
    public string ia_collection_s { get; set; }
    public List<string> isbn { get; set; }
    public string key { get; set; }
    public List<string> language { get; set; }
    public int last_modified_i { get; set; }
    public List<string> lcc { get; set; }
    public string lending_edition_s { get; set; }
    public string lending_identifier_s { get; set; }
    public int number_of_pages_median { get; set; }
    public List<string> oclc { get; set; }
    public string printdisabled_s { get; set; }
    public bool public_scan_b { get; set; }
    public List<string> publish_date { get; set; }
    public List<string> publish_place { get; set; }
    public List<int> publish_year { get; set; }
    public List<string> publisher { get; set; }
    public List<string> seed { get; set; }
    public string subtitle { get; set; }
    public string title { get; set; }
    public string title_sort { get; set; }
    public string title_suggest { get; set; }
    public string type { get; set; }
    public List<string> id_goodreads { get; set; }
    public List<string> id_librarything { get; set; }
    public List<string> subject { get; set; }
    public List<string> place { get; set; }
    public List<string> time { get; set; }
    public List<string> person { get; set; }
    public double ratings_average { get; set; }
    public double ratings_sortable { get; set; }
    public int ratings_count { get; set; }
    public int ratings_count_1 { get; set; }
    public int ratings_count_2 { get; set; }
    public int ratings_count_3 { get; set; }
    public int ratings_count_4 { get; set; }
    public int ratings_count_5 { get; set; }
    public int readinglog_count { get; set; }
    public int want_to_read_count { get; set; }
    public int currently_reading_count { get; set; }
    public int already_read_count { get; set; }
    public List<string> publisher_facet { get; set; }
    public List<string> person_key { get; set; }
    public List<string> time_facet { get; set; }
    public List<string> place_key { get; set; }
    public List<string> person_facet { get; set; }
    public List<string> subject_facet { get; set; }
    public long _version_ { get; set; }
    public List<string> place_facet { get; set; }
    public string lcc_sort { get; set; }
    public List<string> author_facet { get; set; }
    public List<string> subject_key { get; set; }
    public List<string> time_key { get; set; }
}
public class RootBook
{
    public int numFound { get; set; }
    public int start { get; set; }
    public bool numFoundExact { get; set; }
    public List<Book> docs { get; set; }
    public int num_found { get; set; }
    public string q { get; set; }
    public object offset { get; set; }
}
