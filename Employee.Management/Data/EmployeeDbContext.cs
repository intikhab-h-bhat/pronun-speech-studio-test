using Microsoft.EntityFrameworkCore;

namespace Employee.Management.Data
{
    public class EmployeeDbContext:DbContext
    {

        public EmployeeDbContext(DbContextOptions<EmployeeDbContext> options) : base(options)
        {

        }

        public DbSet<Employee> Employees { get; set;}

    }
}
