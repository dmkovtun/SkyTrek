﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Xml.Serialization;
using SkyTrek.Panels;
using SkyTrekVisual.GameItems;
using SkyTrekVisual.GameItems.Helpers;
using SkyTrekVisual.GameItems.Rockets;
using System.Windows.Input;

namespace SkyTrek
{
	/// <summary>
	/// How it works nobody knows
	/// </summary>

	[Serializable]
	public class Engine
	{
		#region TODO - place textblocks somewhere outside of engine

		private TextBlock topScoretext = new TextBlock();

		private TextBlock speed = new TextBlock();

		#endregion
		


		#region OLD

		/// <summary>
		/// List of obstacles			-- TODO fix?
		/// </summary>
		private List<ObstacleFlapppy> ObstactleList = new List<ObstacleFlapppy>();

		/// <summary>
		/// Defines if there is flicker of player on startup
		/// </summary>
		private bool isStartupFlicker = false;

		/// <summary>
		/// Defines whether to use obstacle generation and updating
		/// </summary>
		private bool isObstacleEnabled = false;
		
		/// <summary>
		/// LEGACY
		/// </summary>
		private double topScore = 0;

		#endregion


		/// <summary>
		/// Do not the red button
		/// </summary>
		private DispatcherTimer GameplayTimer;







		/// <summary>
		/// Random for all generation things
		/// </summary>
		private readonly Random r = new Random();

		/// <summary>
		/// Defines whether to show startup screen
		/// </summary>
		private bool isNewGame = true;

		/// <summary>
		/// Defines maximum background object size.
		/// Updatable screen area will be expanded by this value
		/// </summary>
		private int MaxObjectSize = 64;

		/// <summary>
		/// Just a player
		/// </summary>
		public Player CurrentPlayer;

		/// <summary>
		/// 
		/// </summary>
		public SpaceShip PlayerShip;


		/// <summary>
		/// Is raised when player loses a game
		/// </summary>
		public event EventHandler GameOverEvent;



		bool isAutoHeal = true;


		#region Canvases

		//public Canvas BackdroundCanvas { get; set; }
		public Canvas PlayerCanvas { get; set; }
		public Canvas EnemyCanvas { get; set; }
		public Canvas ExplosionCanvas { get; set; }

		public Canvas ShotCanvas { get; set; }




		/// <summary>
		/// Height of updatable screen area
		/// </summary>
		public int Height;

		/// <summary>
		/// Width of updatable screen area
		/// </summary>
		public int Width;


		#endregion



		/// <summary>
		/// Ctor
		/// </summary>
		/// <param name="window"></param>
		public Engine(Player currentPlayer)
		{
			Height = 454 - 48;
			Width = 950 + MaxObjectSize;


			Initialize();

			CollisionDetector.CanvasHeight = Height;

			CurrentPlayer = currentPlayer;
			PlayerShip = currentPlayer.Ship;

		}


		Grid PlayerDamageIndicator;

		public void InitCanvases(GameplayPanel gameplayPanel)
        {
			PlayerDamageIndicator = gameplayPanel.PlayerDamageIndicator;

			//BackdroundCanvas = window.Gameplay.BackdroundCanvas;
			PlayerCanvas = gameplayPanel.PlayerCanvas;
            EnemyCanvas = gameplayPanel.EnemyCanvas;
            ExplosionCanvas = gameplayPanel.ExplosionCanvas;
            ShotCanvas = gameplayPanel.ShotCanvas;

            GunShot.DefaultRocketCanvas = ShotCanvas;

			//window.KeyUp += Window_KeyUp;
			//window.KeyDown += Window_KeyDown;
			//window.MouseDown += Window_MouseDown;

		}










		/// <summary>
		/// Init all variables of engine
		/// </summary>
		public void Initialize()
		{
			GameplayTimer = new DispatcherTimer() { Interval = TimeSpan.FromSeconds(DefaultGameplaySpeed) };
			//GameplayTimer.Tick += BackgroundUpdater;
			GameplayTimer.Tick += UserMovement_Tick;

			GameplayTimer.Tick += PlayerShipUpdater_Tick;
			GameplayTimer.Tick += PlayerShootingUpdater_Tick;

			GameplayTimer.Tick += ExplosionUpdater_Tick;
			GameplayTimer.Tick += EnemyUpdater_Tick;

			GameplayTimer.Tick += EnemyItemDisposingUpdater_Tick;

		}


		private void InitializeCanvases()
		{
			//for(int i = 0; i < StarCount; i++)
			//	BackdroundCanvas.Children.Add(new Star(r.Next() % (Width + MaxObjectSize) - MaxObjectSize, r.Next() % (Height+48)));

			//for(int i = 0; i < PlanetCount; i++)
			//	BackdroundCanvas.Children.Add(new Planet(r.Next() % (Width + MaxObjectSize) - MaxObjectSize, r.Next() % Height));

			//for(int i = 0; i < AsteriodCount; i++)
			//	BackdroundCanvas.Children.Add(new Asteriod(r.Next() % (Width + MaxObjectSize) - MaxObjectSize, r.Next() % Height));

			PlayerCanvas.Children.Add(PlayerShip);
		}




		#region reset

		/// <summary>
		/// Resets game
		/// </summary>
		public void ResetAll()
		{
			//Counter = 0;

			//BackdroundCanvas.Children.Clear();
			EnemyCanvas.Children.Clear();
			PlayerCanvas.Children.Clear();
			ExplosionCanvas.Children.Clear();
			ShotCanvas.Children.Clear();



			PlayerShip.CoordLeft = SpaceShip.Ship_DefaultLeftPosition;
			PlayerShip.CoordBottom = SpaceShip.Ship_DefaultBottomPosition;

			CurrentPlayer.Reset();

			PlayerShip.Visibility = Visibility.Visible;

		}





		#endregion


		public Engine()
		{

		}


	


		#region EXPERIMENTAL part - do not touch the RED button

		#region Gameplay control

		public void Resume()
		{
			GameplayTimer.Start();

			PlayerShip.CurrentGun.Resume();

			foreach(Rocket rocket in ShotCanvas.Children)
				rocket.Resume();

			foreach(Enemy enemy in EnemyCanvas.Children)
				enemy.CurrentGun.Resume();

			PlayerShip.WasHit(0);
		}

		public void Pause()
		{
			GameplayTimer.Stop();

			PlayerShip.CurrentGun.Pause();

			foreach(Rocket rocket in ShotCanvas.Children)
				rocket.Pause();

			foreach(Enemy enemy in EnemyCanvas.Children)
				enemy.CurrentGun.Pause();

			PlayerShip.WasHit(0);
		}


		public bool IsActive()
		{
			return GameplayTimer.IsEnabled;
		}

		#endregion


		private double DefaultGameplaySpeed = 0.01; // 0.5 fow slow

		public bool isMovingUpward = false;
		public bool isMovingDownward = false;
		public bool isMovingForward = false;
		public bool isMovingBackward = false;

		double ForwardIterator = 0;
		double BackwardIterator = 0;
		double UpwardIterator = 0;
		double DownwardIterator = 0;

		public int BulletSpeedModifier { get; private set; } = 1;

		private int BulletRemoveIterator = 0;




		#region Timer updaters for each tick

		///// <summary>
		///// Background canvas updater
		///// </summary>
		///// <param name="sender"></param>
		///// <param name="e"></param>
		//public void BackgroundUpdater(object sender, EventArgs e)
		//{
		//	//-32 ------ Width + 32
		//	//Width + 32 -------- Width + 40

		//	foreach(IGameItem gameplayItem in BackdroundCanvas.Children)
		//	{
		//		if(gameplayItem.CoordLeft < -MaxObjectSize+1)
		//		{
		//			gameplayItem.CoordLeft += Width;
		//			gameplayItem.CoordBottom = r.Next() % Height;

		//			gameplayItem.GenerateType();
		//			gameplayItem.GenerateSize();
		//		}

		//		var l = (gameplayItem as UserControl).ActualHeight;
		//		//gameplayItem.CoordLeft -= (straight_counter * BackgroundSpeedModifier / (gameplayItem as UserControl).ActualHeight) % Width;	// dist
		//		gameplayItem.CoordLeft -= (straight_counter * BackgroundSpeedModifier/100* l) % Width;
		//	}

		//}


		int iterator = 0;


		int countIII = 0;



		private void GameOverWithPlayerExplosion()
		{
			//Pause();

			PlayerShip.Visibility = Visibility.Hidden;

			PlayerShip.StartShipExplosion(ExplosionCanvas);



			





			GameplayTimer.Tick += GameOverLastExplosion_Tick;

		}

		private void GameOverLastExplosion_Tick(object sender, EventArgs e)
		{
			foreach(Explosion exp in ExplosionCanvas.Children)
			{
				exp.GenerateType();
			}

			if(BulletRemoveIterator < ExplosionCanvas.Children.Count)
			{
				if(!(ExplosionCanvas.Children[BulletRemoveIterator] as Explosion).isActive)
					ExplosionCanvas.Children.RemoveAt(BulletRemoveIterator);
			}

			if(ExplosionCanvas.Children.Count==0)
				GameOver();

		}

		private void GameOver()
		{
			Pause();

			PlayerShip.Visibility = Visibility.Hidden;

			GameplayTimer.Tick -= GameOverLastExplosion_Tick;

			GameOverEvent.Invoke(this, null);
			isNewGame = true;
		}




		/// <summary>
		/// Updates explosions
		/// Enemy canvas updater
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		public void EnemyUpdater_Tick(object sender, EventArgs e)
		{
			foreach(Enemy enemy in EnemyCanvas.Children)
			{
				if(enemy.CoordBottom -32 <= PlayerShip.CoordBottom && enemy.CoordBottom + 32 >= PlayerShip.CoordBottom)
				{
					if(r.Next() % 2 == 0)
					{
						enemy.MakeAShot();			
					}
				}

				if(enemy.IsShipCollision(PlayerShip))
				{
					PlayerShip.WasHit(enemy.HitDamage);
                    //for fun)
                    CurrentPlayer.Score.Multiplier = CurrentPlayer.Score.Multiplier / 2;

                    //enemy.HitDamage = 0;
					ExplosionCanvas.Children.Add(new Explosion(enemy, 7));

					enemy.StartShipExplosion(ExplosionCanvas);

					DisposableItems.Add(enemy);


					PlayerDamageIndicator.Opacity += enemy.HitDamage *.1;


					CurrentPlayer.Score.NewShipHit();
				}
				else
					enemy.GoBackward();
			}

			if(iterator++ %100 == 0)
				EnemyCanvas.Children.Add(new Enemy(Width, r.Next() % (Height-64)+5));

		}

		/// <summary>
		/// Updates explosions
		/// Enemy canvas updater
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		public void ExplosionUpdater_Tick(object sender, EventArgs e)
		{
			foreach(Explosion exp in ExplosionCanvas.Children)
			{
				exp.GenerateType();
			}

			if(BulletRemoveIterator < ExplosionCanvas.Children.Count)
			{
				if(!(ExplosionCanvas.Children[BulletRemoveIterator] as Explosion).isActive)
					ExplosionCanvas.Children.RemoveAt(BulletRemoveIterator);
			}


		}



		List<IDestructibleItem> DisposableItems = new List<IDestructibleItem>();


		public void EnemyItemDisposingUpdater_Tick(object sender, EventArgs e)
		{
			foreach(var item in DisposableItems.OfType<Enemy>())
				EnemyCanvas.Children.Remove(item as UIElement);

			foreach(var item in DisposableItems.OfType<Rocket>())
				ShotCanvas.Children.Remove(item as UIElement);

			DisposableItems.Clear();
			
		}

		/// <summary>
		/// Updates bullets
		/// Enemy canvas updater
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		public void PlayerShootingUpdater_Tick(object sender, EventArgs e)
		{

			foreach(Rocket rocket in ShotCanvas.Children)
			{
				if(rocket.CoordLeft > Width || rocket.CoordLeft < - MaxObjectSize)
					DisposableItems.Add(rocket);


				foreach(Enemy enemy in EnemyCanvas.Children)
				{
					if(enemy.CoordBottom - 32 <= rocket.CoordBottom && enemy.CoordBottom + 32 >= rocket.CoordBottom)
					{
						if(enemy.CoordLeft-64<= rocket.CoordLeft && enemy.CoordLeft <= rocket.CoordLeft+32)
						{
							if(enemy.MovementIterator > 8)
								enemy.RenewMovement();

							enemy.ChooseDirectionToRun();

							if(enemy.Direction == MoveDirection.Up)
							{
								//go up 

								int f = (int)(enemy.CoordBottom + 8 * Math.Exp(-((enemy.MovementIterator += 0.5)) * 0.03));

								if(f < Height - PlayerShip.ActualHeight / 2 - 32)
									enemy.CoordBottom = f;
							}
							else if(enemy.Direction == MoveDirection.Down)
							{
								// go down 

								int f = (int)(enemy.CoordBottom - 2 * Math.Exp(-((enemy.MovementIterator -= 0.5)) * 0.02));

								if(f > 0)
									enemy.CoordBottom = f;
							}
							// else do nothing
						}
					}

					if(rocket.IsCollision(enemy))
					{
						enemy.WasHit(rocket.CurrentDamage);

						if(!enemy.IsAlive())
						{
							DisposableItems.Add(enemy);
							enemy.StartShipExplosion(ExplosionCanvas);

							rocket.Bang();

							CurrentPlayer.Score.NewKill();
						}
						else
						{
							rocket.SmallBang();
							ExplosionCanvas.Children.Add(new Explosion(rocket, r.Next() % 10 + 1));

							DisposableItems.Add(rocket);
							CurrentPlayer.Score.NewHit();
						}
					}
				}

				if(rocket.CurrentDirection == Rocket.RocketDirection.Right && rocket.IsCollision(PlayerShip))
				{
					PlayerShip.WasHit(rocket.CurrentDamage);


					PlayerDamageIndicator.Opacity += rocket.CurrentDamage * .1;
					


					//for fun)
					CurrentPlayer.Score.Multiplier = CurrentPlayer.Score.Multiplier / 2;

                    rocket.SmallBang();
					ExplosionCanvas.Children.Add(new Explosion(rocket, r.Next() % 10 + 1));

					DisposableItems.Add(rocket);
				}

			}

		}



		/// <summary>
		/// Updates speed TODO : (and UI)
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		public void PlayerShipUpdater_Tick(object sender, EventArgs e)
		{
			PlayerShip.GenerateType();

			if(isAutoHeal)
				if(countIII++ % 10 == 0)
					PlayerShip.Heal(0.3);

			if(!PlayerShip.IsAlive())
			{
				ExplosionCanvas.Children.Add(new Explosion(PlayerShip, 7));
				GameOverWithPlayerExplosion();
			}

			if(countIII++ % 100 == 0)
				CurrentPlayer.Score.Multiplier -= 0.05;

			if(PlayerDamageIndicator.Opacity>0.001)
				PlayerDamageIndicator.Opacity -= 0.1;


			#region SPEED

			// TODO view model

			speed.Background = new SolidColorBrush(Colors.Transparent);
			speed.Margin = new Thickness(5, 35, 0, 0);
			speed.FontSize = 20.0;
			speed.Foreground = new SolidColorBrush(Colors.White);
			speed.Text = "SPEED: " + PlayerShip.CoordLeft.ToString() + "  ";
	
			#endregion

			#region Startup flicker

			//if(isStartupFlicker)
			//{
			//	if(Counter > 30 || (Counter < 30 && Counter % 5 < 3))
			//		PlayerCanvas.Children.Add(CurrentPlayer);		// fix need to be removed
			//}

			#endregion

		}

		/// <summary>
		/// Updates movement of player
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		public void UserMovement_Tick(object sender, EventArgs e)
		{
			if(isMovingForward && !PlayerShip.IsSpeedMaximum())
			{
				if(isMovingBackward)
					isMovingBackward = false;

				int f = (int)(PlayerShip.CoordLeft + PlayerShip.MaximumSpeed -
					PlayerShip.MaximumSpeed * Math.Exp(-((ForwardIterator += 0.5)) * PlayerShip.ForwardSpeedModifier));

				if(f < PlayerShip.MaximumSpeed)
					PlayerShip.CoordLeft = f;
			}

			if(isMovingBackward && !isMovingForward)
			{
				if(PlayerShip.IsSpeedMinimum())
				{
					PlayerShip.CoordLeft = PlayerShip.MinimumSpeed;
					isMovingBackward = false;
				}
				else
				{
					int v = (int)(PlayerShip.CoordLeft * Math.Exp(-(BackwardIterator += 0.5) * PlayerShip.BackwardSpeedModifier));
					PlayerShip.CoordLeft = v;
				}

				if(Keyboard.IsKeyDown(Key.Space))
					PlayerShip.MakeAShot();
			}

			if(isMovingUpward)
			{
				int f = (int)(PlayerShip.CoordBottom + 8 * Math.Exp(-((UpwardIterator += 0.5)) * 0.3));

				if(f < Height - PlayerShip.ActualHeight/2-32)
					PlayerShip.CoordBottom = f;
			}

			if(isMovingDownward)
			{
				int f = (int)(PlayerShip.CoordBottom - 2 * Math.Exp(-((DownwardIterator -= 0.5)) * 0.2));

				if(f > 0)
					PlayerShip.CoordBottom = f;			
			}
		}


		#endregion





		#region User input event handlers

		/// <summary>
		/// When key is down
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		public void KeyDown(Key keyDown)
		{
            if (Keyboard.IsKeyDown(Key.Space))
				PlayerShip.MakeAShot();

			if(Keyboard.IsKeyDown(Key.Right) || Keyboard.IsKeyDown(Key.D))
				isMovingForward = true;	

			if(Keyboard.IsKeyDown(Key.Up) || Keyboard.IsKeyDown(Key.W))
			{
				isMovingUpward = true;
				UpwardIterator = 0;
			}

			if(Keyboard.IsKeyDown(Key.Down) || Keyboard.IsKeyDown(Key.S))
			{
				isMovingDownward = true;
				DownwardIterator = 0;
			}

			if(isNewGame)
				TryStartNewGame();
		}

		/// <summary>
		/// When key is up
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		public void KeyUp(Key keyUp)
		{
			if(Keyboard.IsKeyUp(Key.Right) || Keyboard.IsKeyDown(Key.D))
			{
				isMovingBackward = true;
				isMovingForward = false;
				BackwardIterator = 0;
				ForwardIterator = 0;
			}

			if(Keyboard.IsKeyUp(Key.Up) || Keyboard.IsKeyDown(Key.W))
				isMovingUpward = false;

			if(Keyboard.IsKeyUp(Key.Down) || Keyboard.IsKeyDown(Key.S))
				isMovingDownward = false;
        }

		/// <summary>
		/// When mouse click
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Window_MouseDown(object sender, MouseButtonEventArgs e)
		{
			//TryStartNewGame();
		}

		#endregion



		/// <summary>
		/// Starts new game if isNewGame is true
		/// </summary>
		private void TryStartNewGame()
		{
			if(isNewGame)
			{
				ResetAll();
				InitializeCanvases();
				

				GameplayTimer.Start();
				
				isNewGame = false;

				PlayerShip.Visibility = Visibility.Visible;

			}

		}



		public void StartGame()
		{
			TryStartNewGame();
		}

		public void ContinueGame()
		{
			//TODO
		}


		#endregion




	}

}
