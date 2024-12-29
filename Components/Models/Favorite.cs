using Clipser.Data;
using SurrealDb.Net.Models;

namespace Clipser.Components.Models;
public class Favorite : Record {
    public required ApplicationUser User { get; set; }
    public Movie? Movie { get; set; }
    public Book? Book { get; set; }
}