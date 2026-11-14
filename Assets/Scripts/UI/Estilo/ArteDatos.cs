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

        public enum Musica { Ninguna, Menu, Configuracion, Investigacion, Victoria, Parcial, Fracaso }

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
                default: return null;
            }
        }
    }
}
