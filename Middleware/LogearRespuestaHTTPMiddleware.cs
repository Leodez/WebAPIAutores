using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace WebAPIAutores.Middleware
{

    public static class LogearRespuestaHTTPMiddlewareExtensions
    {
        public static IApplicationBuilder UseLogearRespuestaHTTP(this IApplicationBuilder app)
        {
            return app.UseMiddleware<LogearRespuestaHTTPMiddleware>();
        }
    }
    public class LogearRespuestaHTTPMiddleware
    {
        private readonly RequestDelegate siguiente;
        private readonly ILogger<LogearRespuestaHTTPMiddleware> logger;

        public LogearRespuestaHTTPMiddleware(RequestDelegate siguiente, ILogger<LogearRespuestaHTTPMiddleware> logger)
        {
            this.siguiente = siguiente;
            this.logger = logger;
        }

        public async Task InvokeAsync(HttpContext contexto)
        {
            using (var ms = new MemoryStream())
            {
                var cuerpoOriginalRespuesta = contexto.Response.Body;
                contexto.Response.Body = ms;

                await siguiente(contexto);

                ms.Seek(0, SeekOrigin.Begin);
                var respuesta = new StreamReader(ms).ReadToEnd();
                ms.Seek(0, SeekOrigin.Begin);

                await ms.CopyToAsync(cuerpoOriginalRespuesta);
                contexto.Response.Body = cuerpoOriginalRespuesta;

                logger.LogInformation(respuesta);
            }

        }
    }
}
