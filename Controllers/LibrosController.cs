using AutoMapper;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebAPIAutores.DTOs;
using WebAPIAutores.Entidades;

namespace WebAPIAutores.Controllers
{
    [ApiController]
    [Route("api/libros")]
    public class LibrosController: ControllerBase
    {
        private readonly ApplicationDbContext context;
        private readonly IMapper mapper;
        private readonly ILogger<LibrosController> logger;

        public LibrosController(ApplicationDbContext context, IMapper mapper, ILogger<LibrosController> logger)
        {
            this.context = context;
            this.mapper = mapper;
            this.logger = logger;
        }

        [HttpGet("{id:int}", Name = "ObtenerLibro")]
        public async Task<ActionResult<LibroDTOConAutores>> Get(int id)
        {
            //string sql = context.Libros.FirstOrDefaultAsync(libro => libro.Id == id).ToQueryString();
            var querySql = context.Libros
                .Include(libroDB => libroDB.AutoresLibros)
                .ThenInclude(autorDB => autorDB.Autor).ToQueryString();
            logger.LogInformation(querySql);

            var libro = await context.Libros
                .Include(libroDB => libroDB.AutoresLibros)
                .ThenInclude(autorDB => autorDB.Autor)
                .FirstOrDefaultAsync(libro => libro.Id == id);

            if (libro == null) return NotFound();

            libro.AutoresLibros = libro.AutoresLibros.OrderBy(x => x.Orden).ToList();

            return mapper.Map<LibroDTOConAutores>(libro);
        }

        [HttpPost]
        public async Task<ActionResult> Post(LibroCreacionDTO libroCreacionDTO)
        {
            if (libroCreacionDTO.AutoresIds == null)
                return BadRequest("No se puede crear un libro sin autores");

            var autoresIds = await context.Autores
                .Where(autorBD => libroCreacionDTO.AutoresIds.Contains(autorBD.Id)).Select(x => x.Id).ToListAsync();

            // Obtiene la instrucción SQL como texto
            string sql = context.Autores
                .Where(autorBD => libroCreacionDTO.AutoresIds.Contains(autorBD.Id)).Select(x => x.Id).ToQueryString();
            logger.LogInformation(sql);

            if (autoresIds.Count != libroCreacionDTO.AutoresIds.Count)
                return BadRequest("No existe uno de los autores enviados.");

            var libro = mapper.Map<Libro>(libroCreacionDTO);

            AsignarOrdenAutores(libro);

            context.Add(libro);
            await context.SaveChangesAsync();

            var libroDTO = mapper.Map<LibroDTO>(libro);
            return CreatedAtRoute("ObtenerLibro", new { id = libro.Id }, libroDTO);
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult> Put(int id, LibroCreacionDTO libroCreacionDTO)
        {
            var libroDB = await context.Libros.Include(x => x.AutoresLibros).FirstOrDefaultAsync(x => x.Id == id);

            if (libroDB == null) return NotFound();

            libroDB = mapper.Map(libroCreacionDTO, libroDB);
            AsignarOrdenAutores(libroDB);
            await context.SaveChangesAsync();
            return NoContent();
        }

        [HttpPatch("{id:int}")]
        public async Task<ActionResult> Patch(int id, JsonPatchDocument<LibroPatchDTO> patchDocument)
        {
            if (patchDocument == null) return BadRequest();

            var libroDB = await context.Libros.FirstOrDefaultAsync(x => x.Id == id);
            if (libroDB == null) return NotFound();

            var libroPatchDTO = mapper.Map<LibroPatchDTO>(libroDB);
            patchDocument.ApplyTo(libroPatchDTO, ModelState);

            var esValido = TryValidateModel(libroPatchDTO);
            if (!esValido) return BadRequest(ModelState);

            mapper.Map(libroPatchDTO, libroDB);

            await context.SaveChangesAsync();
            return NoContent();
        }


        private void AsignarOrdenAutores(Libro libro)
        {
            if (libro.AutoresLibros != null)
                for (int i = 0; i < libro.AutoresLibros.Count; i++)
                {
                    libro.AutoresLibros[i].Orden = i;
                }
        }

        [HttpDelete("{id:int}")]
        public async Task<ActionResult> Delete(int id)
        {
            var existe = await context.Libros.AnyAsync(x => x.Id == id);
            if (!existe)
                return NotFound($"El libro con Id = {id} no existe.");

            context.Remove(new Libro() { Id = id });
            await context.SaveChangesAsync();
            return NoContent();
        }
    }
}
