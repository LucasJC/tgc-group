﻿using Microsoft.DirectX;
using Microsoft.DirectX.DirectInput;
using System;
using System.Collections.Generic;
using System.Drawing;
using TGC.Core.BoundingVolumes;
using TGC.Core.Collision;
using TGC.Core.Direct3D;
using TGC.Core.Example;
using TGC.Core.Geometry;
using TGC.Core.Input;
using TGC.Core.Particle;
using TGC.Core.SceneLoader;
using TGC.Core.Shaders;
using TGC.Core.Terrain;
using TGC.Core.Text;
using TGC.Core.Textures;
using TGC.Core.Utils;
using TGC.Examples.Camara;

namespace TGC.Group.Model
{
    /// <summary>
    ///     Modelo Principal del Juego
    /// </summary>
    public class GameModel : TgcExample
    {
        //tiempo
        private float time = 0;

        public enum DayCycle {DAY, NIGHT};

        //hora del día para controlar ciclos de día y noche
        public int Day { get; set; }
        public int Hour { get; set; }
        public int Minute { get; set; }
        public int Seconds { get; set; }
        public DayCycle Cycle { get; set; }
        private float secondsAuxCounter;

        //Directx device
        Microsoft.DirectX.Direct3D.Device d3dDevice;
        //Loader del framework
        private TgcSceneLoader loader;

        //mundo
        private World MyWorld;

        //jugador
        public Player Player1;

        //reproductor de sonidos
        private SoundPlayer soundPlayer;

        //gui
        private GUI MenuInterface;

        //shaders
        private Microsoft.DirectX.Direct3D.Effect lightEffect;

        //emisor de partículas
        private ParticleEmitter emitter;
        private float emissionTime = 1f;
        private float emittedTime = 0f;
        private bool emit = false;

        //para colisiones
        private TgcPickingRay pickingRay;
        private bool collided = false;
        private Vector3 collisionPoint;
        private InteractiveObject collidedObject = null;

        //mensajes
        private TgcText2D StatusText;
        private TgcText2D DamageText;

        //Semilla para randoms
        public static int RandomSeed { get; } = 666;
        //Dimensiones de cada cuadrante del mapa
        public static int MapLength { get; } = 2000;

        private static Vector3 zeroVector = new Vector3(0f, 0f, 0f);

        /// <summary>
        ///     Constructor del juego.
        /// </summary>
        /// <param name="mediaDir">Ruta donde esta la carpeta con los assets</param>
        /// <param name="shadersDir">Ruta donde esta la carpeta con los shaders</param>
        public GameModel(string mediaDir, string shadersDir) : base(mediaDir, shadersDir)
        {
            Category = Game.Default.Category;
            Name = Game.Default.Name;
            Description = Game.Default.Description;
        }

        /// <summary>
        ///     Se llama una sola vez, al principio cuando se ejecuta el ejemplo.
        ///     Escribir aquí todo el código de inicialización: cargar modelos, texturas, estructuras de optimización, todo
        ///     procesamiento que podemos pre calcular para nuestro juego.
        ///     Borrar el codigo ejemplo no utilizado.
        /// </summary>
        public override void Init()
        {
            //Device de DirectX para crear primitivas.
            d3dDevice = D3DDevice.Instance.Device;
            //Shaders
            lightEffect = TgcShaders.loadEffect(ShadersDir + "CustomLightShader.fx");

            //Instancio el loader del framework
            loader = new TgcSceneLoader();
            //Inicializo cámara
            Camara = new TgcFpsCamera(Input, (MapLength / 2) , - (MapLength / 2), (MapLength / 2), -(MapLength / 2));

            Frustum.updateVolume(D3DDevice.Instance.Device.Transform.View, D3DDevice.Instance.Device.Transform.Projection);
            //genero el mundo
            MyWorld = new World(MediaDir, d3dDevice, loader, Camara, Frustum, MapLength, true);

            //creo usuario
            Player1 = new Player();

            //emisor de partículas
            emitter = new ParticleEmitter(MediaDir + "Textures\\smokeParticle.png", 10);
            emitter.Position = new Vector3(0, 0, 0);
            emitter.MinSizeParticle = 2f;
            emitter.MaxSizeParticle = 5f;
            emitter.ParticleTimeToLive = 1f;
            emitter.CreationFrecuency = 1f;
            emitter.Dispersion = 25;
            emitter.Speed = new Vector3(5f, 5f, 5f);

            //MyWorld.lightEffect = lightEffect;
            //MyWorld.updateEffects();

            //colisiones
            pickingRay = new TgcPickingRay(Input);
            //sonidos
            soundPlayer = new SoundPlayer(DirectSound, MediaDir);

            //gui
            MenuInterface = new GUI(MediaDir, D3DDevice.Instance, Player1, this);

            StatusText = GameUtils.createText("", 0, 0, 20, true);
            StatusText.Color = Color.Beige;
            StatusText.Align = TgcText2D.TextAlign.RIGHT;

            DamageText = GameUtils.createText("", 0, (D3DDevice.Instance.Height * 0.85f), 25, true);
            DamageText.Color = Color.MediumVioletRed;
            DamageText.Align = TgcText2D.TextAlign.CENTER;

        }

        /// <summary>
        ///     Se llama en cada frame.
        ///     Se debe escribir toda la lógica de computo del modelo, así como también verificar entradas del usuario y reacciones
        ///     ante ellas.
        /// </summary>
        public override void Update()
        {
            PreUpdate();

            //controlo tiempo
            time += ElapsedTime;

            updateDayTime(ElapsedTime);

            //reinicio estado de colisiones
            collided = false;
            collidedObject = null;

            MyWorld.update();
            MenuInterface.update();

            //TODO pasar esto a un método --> selección de objetos
            if(Input.keyPressed(Key.LeftArrow))
            {
                Player1.selectPreviousItem();
                soundPlayer.playActionSound(SoundPlayer.Actions.Menu_Next);
            }
            if (Input.keyPressed(Key.RightArrow))
            {
                Player1.selectNextItem();
                soundPlayer.playActionSound(SoundPlayer.Actions.Menu_Next);
            }
            if (Input.keyPressed(Key.E))
            {
                Player1.equipSelectedItem();
                soundPlayer.playActionSound(SoundPlayer.Actions.Menu_Select);
            }
            if (Input.keyPressed(Key.Q))
            {
                Player1.removeInventoryObject(Player1.SelectedItem);
            }
            if (Input.keyPressed(Key.Z))
            {
                Player1.selectForCombination(Player1.SelectedItem);
                soundPlayer.playActionSound(SoundPlayer.Actions.Menu_Select);
            }
            if (Input.keyPressed(Key.C))
            {
                if (!InventoryObject.combineObjects(Player1, Player1.combinationSelection))
                {
                    //falló la combinación
                    soundPlayer.playActionSound(SoundPlayer.Actions.Menu_Wrong);
                }else
                {
                    //comb ok
                    soundPlayer.playActionSound(SoundPlayer.Actions.Success);
                }
            }

            //TODO pasar esto a un método --> testeo de colisiones
            if (Input.buttonPressed(TgcD3dInput.MouseButtons.BUTTON_LEFT))
            {
                pickingRay.updateRay();
                testPicking();
            }

            //controlo tiempos de emisión de partículas
            if (emit)
            {
                if (emittedTime <= emissionTime)
                {
                    emittedTime += ElapsedTime;
                }
                else
                {
                    emit = false;
                    emitter.Position = zeroVector;
                }
            }
        }

        private void testPicking()
        {
            //de los objetos visibles, testeo colisiones con el picking ray
            foreach (InteractiveObject objeto in MyWorld.Objetos)
            {
                if(objeto.mesh.Enabled)
                {
                    collided = TgcCollisionUtils.intersectRayAABB(pickingRay.Ray, objeto.mesh.BoundingBox, out collisionPoint);
                    if (collided)
                    {
                        Vector3 aux = new Vector3(0f, 0f, 0f);
                        aux.Add(Camara.Position);
                        aux.Subtract(objeto.mesh.Position);
                        if (FastMath.Ceiling(aux.Length()) < 50)
                        {
                            collidedObject = objeto;
                            if (collidedObject.getHit(Player1.getDamage()))
                            {
                                DamageText.Text = Player1.getDamage().ToString() + " DMG";
                                MyWorld.destroyObject(collidedObject);
                                List<InventoryObject> drops = collidedObject.getDrops();
                                foreach (InventoryObject invObject in drops)
                                {
                                    //agrego los drops al inventario del usuario
                                    if (!Player1.addInventoryObject(invObject))
                                    {
                                        //no pudo agregar el objeto
                                        StatusText.Text = "No hay espacio en el inventario...";
                                    }
                                }
                            }
                            break;
                        }
                        else
                        {
                            collided = false;
                        }
                    }
                }
            }

            //si hubo colisión
            if (collided)
            {
                //a darle átomos
                emit = true;
                emittedTime = 0;
                emitter.Position = collidedObject.mesh.Position;
            }
        }

        /// <summary>
        ///     actualiza la fecha actual
        /// </summary>
        /// <param name="elapsedTime"></param>
        private void updateDayTime(float elapsedTime)
        {
            secondsAuxCounter += (elapsedTime) * 1000;

            Seconds += (int)secondsAuxCounter;
            secondsAuxCounter = 0;

            if (Seconds >= 60)
            {
                Seconds = 0;

                Minute++;

                if (Minute >= 60)
                {
                    Minute = 0;

                    Hour++;

                    if (Hour == 24)
                    {
                        Hour = 0;
                        Day++;
                    }

                    if(Hour >= 19)
                    {
                        Cycle = GameModel.DayCycle.NIGHT;
                    }else
                    {
                       Cycle = GameModel.DayCycle.DAY;
                    }
                }
            }
            
        }

        /// <summary>
        ///     Se llama cada vez que hay que refrescar la pantalla.
        ///     Escribir aquí todo el código referido al renderizado.
        ///     Borrar todo lo que no haga falta.
        /// </summary>
        public override void Render()
        {
            //Inicio el render de la escena, para ejemplos simples. Cuando tenemos postprocesado o shaders es mejor realizar las operaciones según nuestra conveniencia.
            PreRender();
            //habilito efecto de partículas
            D3DDevice.Instance.ParticlesEnabled = true;
            D3DDevice.Instance.EnableParticles();

            //Variables para el shader
            lightEffect.SetValue("time", time);

            //Dibuja un texto por pantalla
            if(null != Player1.EquippedObject) DrawText.drawText("Objeto equipado: " + Player1.EquippedObject.Type.ToString(), 0, 20, Color.DarkSalmon);
            if (null != Player1.SelectedItem) DrawText.drawText("Objeto seleccionado: (" + Player1.SelectedItemIndex + ")" + Player1.SelectedItem.Type.ToString(), 0, 30, Color.DarkSalmon);

            StatusText.render();
            DamageText.render();
            MyWorld.render();
            MenuInterface.render();
   
            if(null != collidedObject)
            {
                //un objeto fue objetivo de una acción
                soundPlayer.playMaterialSound(collidedObject.material);

                if (!collidedObject.alive)
                {
                    //el objeto debe ser eliminado
                    if(collidedObject.objectType == InteractiveObject.ObjectTypes.Tree)
                    {
                        soundPlayer.playActionSound(SoundPlayer.Actions.TreeFall);
                    }
                }
            }

            //veo si emitir partículas
            if (emit)
            {
                emitter.render(emittedTime);
            }

            //Finaliza el render y presenta en pantalla, al igual que el preRender se debe para casos puntuales es mejor utilizar a mano las operaciones de EndScene y PresentScene
            PostRender();
        }

        /// <summary>
        ///     Se llama cuando termina la ejecución del ejemplo.
        ///     Hacer Dispose() de todos los objetos creados.
        ///     Es muy importante liberar los recursos, sobretodo los gráficos ya que quedan bloqueados en el device de video.
        /// </summary>
        public override void Dispose()
        {
            MyWorld.dispose();
            emitter.dispose();
            MenuInterface.dispose();
        }
    }
}