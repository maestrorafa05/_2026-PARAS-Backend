using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Client;

namespace PARAS.Api.Data;

public class ParasDbContext : DbContext
{
    public ParasDbContext(DbContextOptions<ParasDbContext> options) : base(options)
    {
    }
}