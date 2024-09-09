// 1. First, install these NuGet packages:

// dotnet add package Microsoft.EntityFrameworkCore
// dotnet add package Microsoft.EntityFrameworkCore.Tools
// dotnet add package Microsoft.EntityFrameworkCore.Design

// dotnet add package Microsoft.EntityFrameworkCore.Sqlite

// dotnet tool install --global dotnet-ef

// 2a. 
// dotnet ef migrations add InitialCreate
// dotnet ef migrations remove

// 2b. dotnet ef database update

// 2c. dotnet ef migrations list

// 2d. dotnet ef migrations add __other_migration_name_here
// 2d. dotnet ef database update

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.DependencyInjection;

namespace SimpleEF
{
    // Define your models
    public class Book(string title, int authorId, int publicationYear)
    {
        public int Id { get; set; }
        public string Title { get; set; } = title;
        public int AuthorId { get; set; } = authorId;
        public Author? Author { get; set; }
        public int PublicationYear { get; set; } = publicationYear;
    }

    public class Author(string name)
    {
        public int Id { get; set; }
        public string? Name { get; set; } = name;
        public ICollection<Book> Books { get; set; } = [];
    }

    // Db contexts

    public class BookContext(DbContextOptions<BookContext> options) : DbContext(options)
    {
        public DbSet<Book> Books { get; set; }
        public DbSet<Author> Authors { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Book>()
                .HasOne(b => b.Author)
                .WithMany(a => a.Books)
                .HasForeignKey(b => b.AuthorId);
        }
    }

    // DbContext Factory
    public class BookContextFactory : IDesignTimeDbContextFactory<BookContext>
    {
        public BookContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<BookContext>();
            optionsBuilder.UseSqlite("Data Source=books.db");
            return new BookContext(optionsBuilder.Options);
        }
    }

    // Models and DbContext END

    // Program
    class Program
    {
        static void Main(string[] args)
        {
            var factory = new BookContextFactory();

            using var context = factory.CreateDbContext(args);

            // Ensure database is created and seeded
            context.Database.EnsureCreated();
            SeedData(context);

            // LINQ Examples
            Console.WriteLine("1. All books ordered by title:");
            var allBooks = context.Books.OrderBy(b => b.Title).ToList();
            allBooks.ForEach(b => Console.WriteLine($"- {b.Title}"));

            Console.WriteLine("\n2. Books by George Orwell:");
            var orwellBooks = context.Books
                .Where(b => b.Author != null && b.Author.Name == "George Orwell")
                .ToList();
            orwellBooks.ForEach(b => Console.WriteLine($"- {b.Title}"));

            Console.WriteLine("\n3. Authors and their book count:");
            var authorBookCounts = context.Authors
                .Select(a => new { a.Name, BookCount = a.Books.Count })
                .ToList();
            authorBookCounts.ForEach(abc => Console.WriteLine($"- {abc.Name}: {abc.BookCount} books"));

            Console.WriteLine("\n4. Books published after 1950:");
            var modernBooks = context.Books
                .Where(b => b.PublicationYear > 1950)
                .OrderByDescending(b => b.PublicationYear)
                .ToList();
            modernBooks.ForEach(b => Console.WriteLine($"- {b.Title} ({b.PublicationYear})"));

            Console.WriteLine("\n5. Most prolific author:");
            var prolificAuthor = context.Authors
                .OrderByDescending(a => a.Books.Count)
                .FirstOrDefault();
            Console.WriteLine($"- {prolificAuthor?.Name} with {prolificAuthor?.Books.Count} books");

            Console.WriteLine("\n6. Average publication year of books:");
            var avgYear = context.Books.Average(b => b.PublicationYear);
            Console.WriteLine($"- {avgYear:F2}");

            Console.WriteLine("\n7. Books grouped by publication decade:");
            var booksByDecade = context.Books
                .GroupBy(b => b.PublicationYear / 10 * 10)
                .OrderBy(g => g.Key)
                .Select(g => new { Decade = $"{g.Key}s", Books = g.ToList() })
                .ToList();
            foreach (var decade in booksByDecade)
            {
                Console.WriteLine($"- {decade.Decade}:");
                decade.Books.ForEach(b => Console.WriteLine($"  * {b.Title}"));
            }

        }

        static void SeedData(BookContext context)
        {
            if (!context.Books.Any())
            {
                var orwell = new Author("George Orwell");
                var huxley = new Author("Aldous Huxley");
                var bradbury = new Author("Ray Bradbury");

                context.Authors.AddRange(new List<Author> { orwell, huxley, bradbury });
                context.SaveChanges();

                context.Books.AddRange(new List<Book>
                {
                    new ("1984", orwell.Id, 1949),
                    new ("Animal Farm", orwell.Id, 1945),
                    new ("Brave New World", huxley.Id, 1932),
                    new ("Fahrenheit 451", bradbury.Id, 1953),
                    new ("The Martian Chronicles", bradbury.Id, 1950)
                });

                context.SaveChanges();
            }
        }
    }

}