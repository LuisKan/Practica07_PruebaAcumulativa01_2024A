// ************************************************************************
// Practica 07
// Guerrero Luis
// Fecha de realización: 11/06/2025
// Fecha de entrega: 17/06/2025 
// Resultados:
// - Se creó un servidor TCP que escucha en el puerto 11000.
// - Se acepta conexión de múltiples clientes usando hilos.
// - Se implementó el protocolo personalizado para procesar comandos recibidos.
// - Se utilizó la clase Protocolo para abstraer lógica de negocio (validación de placas, autenticación, contador).
// - Se maneja cada cliente en un hilo independiente y se responde adecuadamente a sus solicitudes.
// ************************************************************************

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Protocolo;

namespace Servidor
{
    class Servidor
    {
        // Escuchador TCP que aceptará conexiones entrantes
        private static TcpListener escuchador;

        // Diccionario para llevar el conteo de solicitudes por dirección IP del cliente
        private static Dictionary<string, int> listadoClientes = new Dictionary<string, int>();

        // Instancia de la clase Protocolo para procesar las solicitudes
        private static Protocolo.Protocolo protocolo = new Protocolo.Protocolo();

        // Punto de entrada del programa
        static void Main(string[] args)
        {
            try
            {
                // Inicializa el servidor para escuchar en cualquier IP del equipo, puerto 11000
                escuchador = new TcpListener(IPAddress.Any, 11000);
                escuchador.Start();
                Console.WriteLine("Servidor inició en el puerto 11000...");

                // Bucle infinito para aceptar múltiples clientes concurrentes
                while (true)
                {
                    // Esperar a que un cliente se conecte
                    TcpClient cliente = escuchador.AcceptTcpClient();
                    Console.WriteLine("Cliente conectado, puerto: {0}", cliente.Client.RemoteEndPoint.ToString());

                    // Crear un nuevo hilo para atender al cliente
                    Thread hiloCliente = new Thread(ManipuladorCliente);
                    hiloCliente.Start(cliente);
                }
            }
            catch (SocketException ex)
            {
                // Mostrar error en consola si ocurre una excepción de red
                Console.WriteLine("Error de socket al iniciar el servidor: " + ex.Message);
            }
            finally
            {
                // Asegurar que se liberen los recursos
                escuchador?.Stop();
            }
        }

        // Método que maneja la comunicación con un cliente específico
        private static void ManipuladorCliente(object obj)
        {
            TcpClient cliente = (TcpClient)obj;
            NetworkStream flujo = null;
            try
            {
                // Obtener el flujo de datos del cliente
                flujo = cliente.GetStream();
                byte[] bufferTx;
                byte[] bufferRx = new byte[1024];
                int bytesRx;

                // Leer datos mientras el cliente mantenga la conexión
                while ((bytesRx = flujo.Read(bufferRx, 0, bufferRx.Length)) > 0)
                {
                    // Decodificar mensaje recibido
                    string mensajeRx = Encoding.UTF8.GetString(bufferRx, 0, bytesRx);

                    // Procesar el mensaje en forma de Pedido
                    Pedido pedido = Pedido.Procesar(mensajeRx);
                    Console.WriteLine("Se recibió: " + pedido);

                    // Obtener dirección IP del cliente para control de conteo
                    string direccionCliente = cliente.Client.RemoteEndPoint.ToString();

                    // Resolver el pedido utilizando la lógica del protocolo
                    Respuesta respuesta = protocolo.ResolverPedido(pedido, direccionCliente, listadoClientes);
                    Console.WriteLine("Se envió: " + respuesta);

                    // Enviar la respuesta al cliente
                    bufferTx = Encoding.UTF8.GetBytes(respuesta.ToString());
                    flujo.Write(bufferTx, 0, bufferTx.Length);
                }

            }
            catch (SocketException ex)
            {
                // Mostrar error en caso de fallo de comunicación con el cliente
                Console.WriteLine("Error de socket al manejar el cliente: " + ex.Message);
            }
            finally
            {
                // Liberar recursos
                flujo?.Close();
                cliente?.Close();
            }
        }
    }
}
