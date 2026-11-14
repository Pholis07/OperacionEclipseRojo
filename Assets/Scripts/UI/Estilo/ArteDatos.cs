using System.Collections.Generic;
using UnityEngine;

namespace SpaceMission
{
    /// <summary>
    /// Imágenes y audio del juego en un solo asset (Assets/Arte/Arte.asset), separado de los datos de la misión.
    /// Cualquier audio puede faltar: el juego funciona igual, en silencio.
    /// </summary>
    [CreateAssetMenu(fileName = "Arte", menuName = "SpaceMission/Arte")]
    public class ArteDatos : ScriptableObject
    {
        [System.Serializable]
        public class Evidencia
        {
            public string instrumentoId;
            public Texture2D imagen;
            public string credito;
        }

        [System.Serializable]
        public class Voz
        {
            public string transmisionId;
            public AudioClip clip;
        }

        public enum Sonido { Hover, Click, Elegir, Quitar, Pestana, Error, LuzVerde, LuzRoja, Lanzamiento, Senal }

        // Lanzamiento va al final: el valor se guarda en las escenas y no debe cambiar el de los demás.
        public enum Musica { Ninguna, Menu, Configuracion, Investigacion, Victoria, Parcial, Fracaso, Lanzamiento }

        /// <summary>Capa de ambiente bajo la música.</summary>
        public enum Ambiente { Tormenta, Control, Orbital }

        public AudioClip Clip(Ambiente a) =>
            a == Ambiente.Control && ambienteControl != null ? ambienteControl :
            a == Ambiente.Orbital && ambienteOrbital != null ? ambienteOrbital : ambienteTormenta;

        /// <summary>Sonidos de la cinemática de lanzamiento y del escaneo orbital (todos opcionales).</summary>
        public enum Efecto
        {
            CuentaAtras, Despegue, Separacion, Despliegue, Transferencia, Propulsores, Servo, FijarObjetivo,
            EscaneoTermica, EscaneoRadar, EscaneoAltaRes, EscaneoAtmosferico, EscaneoEspectrometro, DatosRecibidos
        }

        [Header("Imágenes")]
        public List<Evidencia> evidencias = new List<Evidencia>();
        public Texture2D imagenMision;
        public string creditoMision;

        [Header("Música y ambiente")]
        public AudioClip musicaMenu;
        public AudioClip musicaConfiguracion;
        public AudioClip musicaInvestigacion;
        public AudioClip musicaVictoria;
        public AudioClip musicaParcial;
        public AudioClip musicaFracaso;
        public AudioClip ambienteTormenta;

        [Header("Sonidos de la interfaz")]
        public AudioClip hover;
        public AudioClip click;
        public AudioClip elegir;
        public AudioClip quitar;
        public AudioClip pestana;
        public AudioClip error;
        public AudioClip luzVerde;
        public AudioClip luzRoja;
        public AudioClip lanzamiento;
        public AudioClip senal;

        [Header("Cinemática y escaneo orbital")]
        public AudioClip musicaLanzamiento;
        public AudioClip cuentaAtras;
        public AudioClip despegue;
        public AudioClip separacion;
        public AudioClip despliegue;
        public AudioClip transferencia;
        public AudioClip propulsores;
        public AudioClip servo;
        public AudioClip fijarObjetivo;
        public AudioClip escaneoTermica;
        public AudioClip escaneoRadar;
        public AudioClip escaneoAltaRes;
        public AudioClip escaneoAtmosferico;
        public AudioClip escaneoEspectrometro;
        public AudioClip ambienteOrbital;
        public AudioClip ambienteControl;

        [Header("Voces de las transmisiones")]
        public List<Voz> voces = new List<Voz>();

        public Evidencia DeInstrumento(string id) => evidencias.Find(e => e.instrumentoId == id);

        public AudioClip VozDe(string transmisionId) => voces.Find(v => v.transmisionId == transmisionId)?.clip;

        public AudioClip Clip(Sonido s)
        {
            switch (s)
            {
                case Sonido.Hover: return hover;
                case Sonido.Click: return click;
                case Sonido.Elegir: return elegir;
                case Sonido.Quitar: return quitar;
                case Sonido.Pestana: return pestana;
                case Sonido.Error: return error;
                case Sonido.LuzVerde: return luzVerde;
                case Sonido.LuzRoja: return luzRoja;
                case Sonido.Lanzamiento: return lanzamiento;
                default: return senal;
            }
        }

        public AudioClip Clip(Efecto e)
        {
            switch (e)
            {
                case Efecto.CuentaAtras: return cuentaAtras;
                case Efecto.Despegue: return despegue != null ? despegue : lanzamiento;
                case Efecto.Separacion: return separacion;
                case Efecto.Despliegue: return despliegue;
                case Efecto.Transferencia: return transferencia;
                case Efecto.Propulsores: return propulsores;
                case Efecto.Servo: return servo;
                case Efecto.FijarObjetivo: return fijarObjetivo;
                case Efecto.EscaneoTermica: return escaneoTermica;
                case Efecto.EscaneoRadar: return escaneoRadar;
                case Efecto.EscaneoAltaRes: return escaneoAltaRes;
                case Efecto.EscaneoAtmosferico: return escaneoAtmosferico;
                case Efecto.EscaneoEspectrometro: return escaneoEspectrometro;
                default: return senal;
            }
        }

        public static Efecto EscaneoDe(string instrumentoId)
        {
            switch (instrumentoId)
            {
                case "termica": return Efecto.EscaneoTermica;
                case "radar": return Efecto.EscaneoRadar;
                case "alta_res": return Efecto.EscaneoAltaRes;
                case "atmosferico": return Efecto.EscaneoAtmosferico;
                default: return Efecto.EscaneoEspectrometro;
            }
        }

        public AudioClip Clip(Musica m)
        {
            switch (m)
            {
                case Musica.Menu: return musicaMenu;
                case Musica.Configuracion: return musicaConfiguracion;
                case Musica.Investigacion: return musicaInvestigacion;
                case Musica.Victoria: return musicaVictoria;
                case Musica.Parcial: return musicaParcial;
                case Musica.Fracaso: return musicaFracaso;
                case Musica.Lanzamiento: return musicaLanzamiento;
                default: return null;
            }
        }
    }
}
