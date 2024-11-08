// <copyright file="LogicaReintentos.cs" company="Canella S.A.">
// Desarrollado por Canella S.A. Todos los derechos reservados.
// </copyright>

namespace Banca.Sesion.Redis
{
    using System;
    using System.Threading;
#if !NET461
    using System.Threading.Tasks;
#endif

    /// <summary>
    /// Lógica para reintentar una operación durante un tiempo definido.
    /// </summary>
    internal static class LogicaReintentos
    {
        /// <summary>
        /// Reintenta la función solicitada durante el tiempo indicado como máximo.
        /// </summary>
        /// <param name="funcion">La función a reintentar.</param>
        /// <param name="tiempoEsperaReintentos">El tiempo máximo durante el cual se reintenta la operación antes de lanzar la excepción que
        /// obliga los reintentos.</param>
        /// <typeparam name="TResultado">El tipo de dato que se espera como resultado de la ejecución de la tarea.</typeparam>
#if !NET461
        /// <returns>Una tarea cuyo resultado es el resultado de la función.</returns>
        public static async Task<TResultado> EjecutarFuncionAsync<TResultado>(
            Func<Task<TResultado>> funcion, TimeSpan tiempoEsperaReintentos)
#else
        /// <returns>El resultado de la función.</returns>
        public static TResultado EjecutarFuncion<TResultado>(Func<TResultado> funcion, TimeSpan tiempoEsperaReintentos)
#endif
        {
            int milisegundosReintento = 20;
            DateTime horaInicio = DateTime.Now;
            byte contadorReintentos = 0;
            while (true)
            {
                try
                {
#if !NET461
                    return await funcion();
#else
                    return funcion();
#endif
                }
                catch (Exception)
                {
                    TimeSpan tiempoTranscurrido = DateTime.Now - horaInicio;
                    if (tiempoEsperaReintentos < tiempoTranscurrido)
                    {
                        throw;
                    }
                    else
                    {
                        int tiempoEsperaRestante = (int)(tiempoEsperaReintentos.TotalMilliseconds - tiempoTranscurrido.TotalMilliseconds);
                        if (tiempoEsperaRestante < milisegundosReintento)
                        {
                            milisegundosReintento = tiempoEsperaRestante;
                        }
                    }

                    Thread.Sleep(milisegundosReintento);
                    milisegundosReintento = 1000;
                }

                contadorReintentos++;
            }
        }
    }
}
