using Employee.Management.Data;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

 

namespace Employee.Management.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EmployeeController : ControllerBase
    {
        private readonly EmployeeDbContext _employeeDbContext;
        public EmployeeController(EmployeeDbContext employeeDbContext)
        {
            _employeeDbContext = employeeDbContext;
        }


        [HttpGet]
      public   ActionResult GetAllEmplyoees()
        {

            var getAllEmp= _employeeDbContext.Employees.ToList();

            return Ok(getAllEmp);

        }

        [HttpPost]
        public ActionResult CreateEmployee([FromBody] Data.Employee emp ) 
        {

            _employeeDbContext.Employees.Add( emp );
            _employeeDbContext.SaveChanges();

            return Ok();

        }

        


    }
}
