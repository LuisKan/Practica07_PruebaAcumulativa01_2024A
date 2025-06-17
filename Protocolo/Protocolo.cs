// ************************************************************************
// Practica 07
// Guerrero Luis
// Fecha de realización: 11/06/2025
// Fecha de entrega: 17/06/2025 
// Resultados:
// - Se definieron las clases `Pedido` y `Respuesta` para estructurar la comunicación cliente-servidor.
// - Se implementó la clase `Protocolo` que centraliza la lógica de transmisión y procesamiento de comandos.
// - Se codificó la validación de placas vehiculares y su conversión a un indicador binario de día.
// - Se creó un sistema de autenticación básica con usuario y contraseña.
// - Se implementó un contador por cliente que permite rastrear el número de consultas realizadas.
// ************************************************************************

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;

namespace Protocolo
{
    // Clase que representa una solicitud enviada por el cliente
    public class Pedido
    {
        public string Comando { get; set; }         // Comando principal
        public string[] Parametros { get; set; }    // Lista de parámetros

        // Método estático para convertir un mensaje plano en un objeto Pedido
        public static Pedido Procesar(string mensaje)
        {
            var partes = mensaje.Split(' ');
            return new Pedido
            {
                Comando = partes[0].ToUpper(),
                Parametros = partes.Skip(1).ToArray()
            };
        }

        // Representación en texto del pedido
        public override string ToString()
        {
            return $"{Comando} {string.Join(" ", Parametros)}";
        }
    }

    // Clase que representa una respuesta enviada por el servidor
    public class Respuesta
    {
        public string Estado { get; set; }   // "OK" o "NOK"
        public string Mensaje { get; set; }  // Mensaje descriptivo

        // Representación en texto de la respuesta
        public override string ToString()
        {
            return $"{Estado} {Mensaje}";
        }
    }

    // Clase que encapsula toda la lógica del protocolo de comunicación
    public class Protocolo
    {
        // Método que permite enviar un Pedido y recibir una Respuesta por flujo TCP
        public Respuesta HazOperacion(Pedido pedido, NetworkStream flujo)
        {
            if (flujo == null)
                return null;

            try
            {
                // Enviar datos al servidor
                byte[] bufferTx = Encoding.UTF8.GetBytes(pedido.ToString());
                flujo.Write(bufferTx, 0, bufferTx.Length);

                // Esperar respuesta del servidor
                byte[] bufferRx = new byte[1024];
                int bytesRx = flujo.Read(bufferRx, 0, bufferRx.Length);

                // Convertir respuesta a objeto Respuesta
                string mensaje = Encoding.UTF8.GetString(bufferRx, 0, bytesRx);
                var partes = mensaje.Split(' ');
                return new Respuesta
                {
                    Estado = partes[0],
                    Mensaje = string.Join(" ", partes.Skip(1))
                };
            }
            catch
            {
                return null; // Error en la comunicación
            }
        }

        // Método que resuelve un pedido recibido por el servidor
        public Respuesta ResolverPedido(Pedido pedido, string direccionCliente, Dictionary<string, int> listadoClientes)
        {
            // Respuesta por defecto
            Respuesta respuesta = new Respuesta
            {
                Estado = "NOK",
                Mensaje = "Comando no reconocido"
            };

            // Evaluar el tipo de comando recibido
            switch (pedido.Comando)
            {
                case "INGRESO":
                    // Validar credenciales
                    if (pedido.Parametros.Length == 2 &&
                        pedido.Parametros[0] == "root" &&
                        pedido.Parametros[1] == "admin20")
                    {
                        // Aleatoriamente conceder o negar acceso
                        respuesta = new Random().Next(2) == 0
                            ? new Respuesta { Estado = "OK", Mensaje = "ACCESO_CONCEDIDO" }
                            : new Respuesta { Estado = "NOK", Mensaje = "ACCESO_NEGADO" };
                    }
                    else
                    {
                        respuesta.Mensaje = "ACCESO_NEGADO";
                    }
                    break;

                case "CALCULO":
                    // Validar placa y calcular el día correspondiente
                    if (pedido.Parametros.Length == 3)
                    {
                        string placa = pedido.Parametros[2];
                        if (ValidarPlaca(placa))
                        {
                            byte indicador = ObtenerIndicadorDia(placa);
                            respuesta = new Respuesta
                            {
                                Estado = "OK",
                                Mensaje = $"{placa} {indicador}"
                            };
                            // Registrar consulta del cliente
                            ContadorCliente(direccionCliente, listadoClientes);
                        }
                        else
                        {
                            respuesta.Mensaje = "Placa no válida";
                        }
                    }
                    break;

                case "CONTADOR":
                    // Devolver el número de solicitudes hechas por el cliente
                    if (listadoClientes.ContainsKey(direccionCliente))
                    {
                        respuesta = new Respuesta
                        {
                            Estado = "OK",
                            Mensaje = listadoClientes[direccionCliente].ToString()
                        };
                    }
                    else
                    {
                        respuesta.Mensaje = "No hay solicitudes previas";
                    }
                    break;
            }

            return respuesta;
        }

        // Validación de formato de placas: 3 letras seguidas de 4 dígitos
        private bool ValidarPlaca(string placa)
        {
            return Regex.IsMatch(placa, @"^[A-Z]{3}[0-9]{4}$");
        }

        // Determina el día de circulación según el último dígito de la placa
        private byte ObtenerIndicadorDia(string placa)
        {
            int ultimo = int.Parse(placa.Substring(placa.Length - 1, 1));

            switch (ultimo)
            {
                case 1:
                case 2: return 32; // Lunes
                case 3:
                case 4: return 16; // Martes
                case 5:
                case 6: return 8;  // Miércoles
                case 7:
                case 8: return 4;  // Jueves
                case 9:
                case 0: return 2;  // Viernes
                default: return 0;
            }
        }

        // Registra o incrementa la cantidad de pedidos por cliente
        private void ContadorCliente(string direccion, Dictionary<string, int> lista)
        {
            if (lista.ContainsKey(direccion))
                lista[direccion]++;
            else
                lista[direccion] = 1;
        }
    }
}
