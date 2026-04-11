using CareerSEA.Data;
using Microsoft.EntityFrameworkCore;

namespace CareerSEA.Tests.Helpers;

public static class TestDbFactory
{
    public static CareerSEADbContext Create()
    {
        var options = new DbContextOptionsBuilder<CareerSEADbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new CareerSEADbContext(options);
    }
}