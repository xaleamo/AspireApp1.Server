// using Microsoft.AspNetCore.Mvc;
// using AspireApp1.Server.DTO;
// using AspireApp1.Server.Services;
//
// namespace AspireApp1.Server.Controllers
// {
//     [ApiController]
//     [Route("api/[controller]")]
//     public class OrdersController : ControllerBase
//     {
//         private readonly OrderService _service;
//
//         public OrdersController(OrderService service)
//         {
//             _service = service;
//         }
//
//         // GET /api/orders          -> all orders (admin)
//         // GET /api/orders?email=x  -> orders for one customer
//         [HttpGet]
//         public IActionResult GetAll([FromQuery] string? email)
//         {
//             if (!string.IsNullOrWhiteSpace(email))
//                 return Ok(_service.GetByCustomer(email));
//
//             return Ok(_service.GetAll());
//         }
//
//         [HttpGet("{id}")]
//         public IActionResult GetById(int id)
//         {
//             var dto = _service.GetById(id);
//             if (dto == null) return NotFound();
//             return Ok(dto);
//         }
//
//         [HttpPost]
//         public IActionResult Create([FromBody] CreateOrderDtoInput dtoInput)
//         {
//             var created = _service.Create(dtoInput);
//             return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
//         }
//
//         // Customer cancels their order
//         [HttpDelete("{id}")]
//         public IActionResult Delete(int id)
//         {
//             if (!_service.Delete(id)) return NotFound();
//             return NoContent();
//         }
//
//         // Admin archives an order
//         [HttpPut("{id}/archive")]
//         public IActionResult Archive(int id)
//         {
//             if (!_service.Archive(id)) return NotFound();
//             return NoContent();
//         }
//     }
// }
