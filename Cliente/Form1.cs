// ************************************************************************
// Practica 07
// Guerrero Luis
// Fecha de realización: 11/06/2025
// Fecha de entrega: 17/06/2025 
// Resultados:
// - Se implementó un formulario cliente capaz de conectarse por TCP al servidor.
// - Se diseñó un sistema de autenticación con verificación de credenciales.
// - Se procesó la validación de placas para determinar el día de circulación.
// - Se recibieron respuestas codificadas desde el servidor para habilitar checkboxes.
// - Se incorporó una opción para visualizar el número de consultas realizadas.
// ************************************************************************

using System;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Net.Sockets;
using Protocolo;

namespace Cliente
{
    public partial class FrmValidador : Form
    {
        // Cliente TCP para conectarse al servidor
        private TcpClient remoto;

        // Flujo de red para transmisión de datos
        private NetworkStream flujo;

        // Instancia del protocolo para manejar operaciones
        private Protocolo.Protocolo protocolo = new Protocolo.Protocolo();

        // Constructor del formulario
        public FrmValidador()
        {
            InitializeComponent();
        }

        // Evento que se ejecuta al cargar el formulario
        private void FrmValidador_Load(object sender, EventArgs e)
        {
            try
            {
                // Intentar establecer conexión TCP al servidor en el puerto 11000
                remoto = new TcpClient("127.0.0.1", 11000);
                flujo = remoto.GetStream(); // Obtener flujo de datos
            }
            catch (SocketException ex)
            {
                // Mostrar mensaje si no se pudo conectar
                MessageBox.Show("No se pudo establecer conexión " + ex.Message, "ERROR");
            }

            // Deshabilitar panel de placa y checks de días al inicio
            panPlaca.Enabled = false;
            chkLunes.Enabled = false;
            chkMartes.Enabled = false;
            chkMiercoles.Enabled = false;
            chkJueves.Enabled = false;
            chkViernes.Enabled = false;
            chkDomingo.Enabled = false;
            chkSabado.Enabled = false;
        }

        // Evento que se ejecuta al hacer clic en el botón "Iniciar"
        private void btnIniciar_Click(object sender, EventArgs e)
        {
            string usuario = txtUsuario.Text;      // Obtener nombre de usuario
            string contraseña = txtPassword.Text;  // Obtener contraseña

            // Validar campos vacíos
            if (usuario == "" || contraseña == "")
            {
                MessageBox.Show("Se requiere el ingreso de usuario y contraseña", "ADVERTENCIA");
                return;
            }

            // Crear objeto Pedido para comando de ingreso
            Pedido pedido = new Pedido
            {
                Comando = "INGRESO",
                Parametros = new[] { usuario, contraseña }
            };

            // Enviar pedido al servidor y obtener respuesta
            Respuesta respuesta = HazOperacion(pedido);
            if (respuesta == null)
            {
                MessageBox.Show("Hubo un error", "ERROR");
                return;
            }

            // Evaluar la respuesta del servidor
            if (respuesta.Estado == "OK" && respuesta.Mensaje == "ACCESO_CONCEDIDO")
            {
                // Habilitar panel de placa y deshabilitar login
                panPlaca.Enabled = true;
                panLogin.Enabled = false;
                MessageBox.Show("Acceso concedido", "INFORMACIÓN");
                txtModelo.Focus(); // Enfocar campo modelo
            }
            else
            {
                // Mostrar error de acceso
                panPlaca.Enabled = false;
                panLogin.Enabled = true;
                MessageBox.Show("No se pudo ingresar, revise credenciales", "ERROR");
                txtUsuario.Focus();
            }
        }

        // Método reutilizable para enviar un Pedido y recibir una Respuesta usando el protocolo
        private Respuesta HazOperacion(Pedido pedido)
        {
            return protocolo.HazOperacion(pedido, flujo);
        }

        // Evento que se ejecuta al hacer clic en "Consultar"
        private void btnConsultar_Click(object sender, EventArgs e)
        {
            // Leer los campos del formulario
            string modelo = txtModelo.Text;
            string marca = txtMarca.Text;
            string placa = txtPlaca.Text;

            // Crear un nuevo pedido con el comando CALCULO
            Pedido pedido = new Pedido
            {
                Comando = "CALCULO",
                Parametros = new[] { modelo, marca, placa }
            };

            // Enviar pedido y recibir respuesta
            Respuesta respuesta = HazOperacion(pedido);
            if (respuesta == null)
            {
                MessageBox.Show("Hubo un error", "ERROR");
                return;
            }

            // Procesar resultado de la placa
            byte resultado = 0;
            var partes = respuesta.Mensaje.Split(' ');
            if (partes.Length > 1)
                byte.TryParse(partes[1], out resultado);

            // Mostrar el día correspondiente desmarcando los demás
            chkLunes.Checked = (resultado == 0b00100000);
            chkMartes.Checked = (resultado == 0b00010000);
            chkMiercoles.Checked = (resultado == 0b00001000);
            chkJueves.Checked = (resultado == 0b00000100);
            chkViernes.Checked = (resultado == 0b00000010);
        }

        // Evento que se ejecuta al hacer clic en "Número de Consultas"
        private void btnNumConsultas_Click(object sender, EventArgs e)
        {
            // Crear pedido con el comando CONTADOR
            Pedido pedido = new Pedido
            {
                Comando = "CONTADOR",
                Parametros = new[] { "mensaje" }
            };

            // Enviar y procesar respuesta
            Respuesta respuesta = HazOperacion(pedido);
            if (respuesta == null)
            {
                MessageBox.Show("Hubo un error", "ERROR");
                return;
            }

            // Mostrar cantidad de consultas previas
            MessageBox.Show("El número de pedidos recibidos en este cliente es " + respuesta.Mensaje, "INFORMACIÓN");
        }

        // Evento que se ejecuta al cerrar el formulario
        private void FrmValidador_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Cerrar conexiones TCP si están abiertas
            flujo?.Close();
            remoto?.Close();
        }
    }
}
