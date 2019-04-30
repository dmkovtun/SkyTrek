﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using SkyTrekVisual.GameItems;

namespace SkyTrek
{
	/// <summary>
	/// How it works nobody knows
	/// </summary>
	public class Engine
	{
		#region TODO - place textblocks somewhere outside of engine


		public TextBlock topScoretext = new TextBlock();
		public TextBlock speed = new TextBlock();



		#endregion


		private int straight_counter = 0;

		private double topScore = 0;



		#region Background items 

		private List<IGameItem> BackgroundItems = new List<IGameItem>();

		private static int StarCount = 300;
		private static int PlanetCount = 7;
		private static int AsteriodCount = 17;


		#endregion



		



		#region Obstacles - LEGACY (flappy mode)

		private readonly double ob_GapEnd = 60;
		private readonly double ob_Width = 50.0;
		private readonly double ob_GapBase = 200.0;
		private readonly double ob_Speed = 4.0;

		#endregion





		#region Player properties	TODO - make a class for player and his ship

	













		#endregion







		public Canvas BackdroundCanvas { get; set; }
		public Canvas PlayerCanvas { get; set; }
		public Canvas EnemyCanvas { get; set; }

		public Canvas ExplosionCanvas { get; set; }

		

		private DispatcherTimer GameplayTimer;


		private int Counter = 0;



		/// <summary>
		/// Random for all generation things
		/// </summary>
		private readonly Random r = new Random();

		/// <summary>
		/// List of obstacles			-- TODO fix?
		/// </summary>
		private List<ObstacleFlapppy> ObstactleList = new List<ObstacleFlapppy>();

		/// <summary>
		/// Defines whether to show startup screen
		/// </summary>
		private bool isNewGame = true;

		/// <summary>
		/// Height of updatable screen area
		/// </summary>
		private int Height;

	

		/// <summary>
		/// Width of updatable screen area
		/// </summary>
		private int Width;


		/// <summary>
		/// Defines whether to use obstacle generation and updating
		/// </summary>
		private bool isObstacleEnabled = false;


		/// <summary>
		/// Defines maximum background object size.
		/// Updatable screen area will be expanded by this value
		/// </summary>
		private int MaxObjectSize = 64;


		/// <summary>
		/// Defines how much background items will change their position every tick
		/// </summary>
		double StarSpeedModifier = 2.0;     // def 1.5






		/// <summary>
		/// Defines if there is flicker of player on startup
		/// </summary>
		private bool isStartupFlicker = false;		





		/// <summary>
		/// Just a player
		/// </summary>
		public Player CurrentPlayer;



		/// <summary>
		/// Is raised when player loses a game
		/// </summary>
		public event EventHandler GameOverEvent;








		/// <summary>
		/// Ctor
		/// </summary>
		/// <param name="window"></param>
		public Engine(MainWindow window)
		{
			BackdroundCanvas = window.BackdroundCanvas;
			PlayerCanvas = window.PlayerCanvas;
			EnemyCanvas = window.EnemyCanvas;
			ExplosionCanvas = window.ExplosionCanvas;

			Height = (int)(BackdroundCanvas.ActualHeight + MaxObjectSize);
			Width = (int)(BackdroundCanvas.ActualWidth + MaxObjectSize);

			PlayerShot.Height = Height;


			window.KeyUp += Window_KeyUp;
			window.KeyDown += Window_KeyDown;
			window.MouseDown += Window_MouseDown;


			Explosion.InitializeImages();





			var b = new Enemy(300, 300, Height);



			EnemyCanvas.Children.Add(b);



		}
		




		/// <summary>
		/// Init all variables of engine
		/// </summary>
		public void Initialize()
		{
			for(int i = 0; i < StarCount; i++)
				BackgroundItems.Add(new Star(r.Next() % (Width + MaxObjectSize) - MaxObjectSize, r.Next() % Height));

			for(int i = 0; i < PlanetCount; i++)
				BackgroundItems.Add(new Planet(r.Next() % (Width + MaxObjectSize) - MaxObjectSize, r.Next() % Height));

			for(int i = 0; i < AsteriodCount; i++)
				BackgroundItems.Add(new Asteriod(r.Next() % (Width + MaxObjectSize) - MaxObjectSize, r.Next() % Height));

			

			GameplayTimer = new DispatcherTimer() { Interval = TimeSpan.FromSeconds(DefaultGameplaySpeed) };
			GameplayTimer.Tick += BackgroundUpdater;
			GameplayTimer.Tick += UserMovement_Tick;

			GameplayTimer.Tick += PlayerShipUpdater_Tick;
			GameplayTimer.Tick += PlayerShootingUpdater_Tick;

			GameplayTimer.Tick += ExplosionUpdater_Tick;
			GameplayTimer.Tick += EnemyUpdater_Tick;


			CurrentPlayer = new Player();
		}


		private void InitializeCanvas()
		{
			foreach(var item in BackgroundItems)
				BackdroundCanvas.Children.Add(item as UIElement);

			PlayerCanvas.Children.Add(CurrentPlayer);
			PlayerCanvas.Children.Add(speed);
		}





		/// <summary>
		/// Updates players` position on canvas
		/// </summary>
		private void UpdatePlayerPosition()
		{
			CurrentPlayer.SetValue(Canvas.TopProperty, (Height - CurrentPlayer.CurrentLift) + 0.1);
			CurrentPlayer.SetValue(Canvas.LeftProperty, CurrentPlayer.CurrentSpeed+0.1);
		}



		/// <summary>
		/// Resets game
		/// </summary>
		public void ResetAll()
		{
			//Counter = 0;

			//ObstactleList.Clear();
			//for(int i = 0; i < Partitions; i++)
			//	ObstactleList.Add(new Obstacle() { Height = r.NextDouble(), Left = 500 + (Width + ob_Width) * (i / Partitions), 
			//Neg = (r.Next() % 2) * 2 - 1 });

			
			CurrentPlayer.CurrentSpeed = Player.Player_DefaultXPosition;
			CurrentPlayer.CurrentLift = Player.Player_DefaultYPosition;
		}

		/// <summary>
		/// Collision detection method
		/// </summary>
		/// <param name="r1"></param>
		/// <param name="r2"></param>
		/// <returns></returns>
		bool IsCollision(Rectangle r1, Rectangle r2)
		{
			double r1L = (double)r1.GetValue(Canvas.LeftProperty);
			double r1T = (double)r1.GetValue(Canvas.TopProperty);
			double r1R = r1L + r1.Width;
			double r1B = r1T + r1.Height;

			double r2L = (double)r2.GetValue(Canvas.LeftProperty);
			double r2T = (double)r2.GetValue(Canvas.TopProperty);
			double r2R = r2L + r2.Width;
			double r2B = r2T + r2.Height;

			if(r1T < 0)
				return true;
			if(r1B > Height)
				return true;

			return r1R > r2L && r1L < r2R && r1B > r2T && r1T < r2B;
		}




	






		#region EXPERIMENTAL part - do not touch the RED button


		public void Resume()
		{
			GameplayTimer.Start();
		}

		public void Pause()
		{
			GameplayTimer.Stop();
		}


		public bool IsActive()
		{
			return GameplayTimer.IsEnabled;
		}




		private double DefaultGameplaySpeed = 0.01;	// 0.5 fow slow




		







		private bool isMovingUpward = false;
		private bool isMovingDownward = false;
		private bool isMovingForward = false;
		private bool isMovingBackward = false;


		double ForwardIterator = 0;
		double BackwardIterator = 0;
		double UpwardIterator = 0;
		double DownwardIterator = 0;

		public int BulletSpeedModifier { get; private set; } = 1;

		private int BulletRemoveIterator = 0;







		/// <summary>
		/// Background canvas updater
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		public void BackgroundUpdater(object sender, EventArgs e)
		{
			//-32 ------ Width + 32
			//Width + 32 -------- Width + 40

			foreach(IGameItem gameplayItem in BackgroundItems)
			{
				if(gameplayItem.CoordX - straight_counter * StarSpeedModifier < -MaxObjectSize)
				{
					gameplayItem.CoordX += Width;
					gameplayItem.CoordY = r.Next() % Height;

					gameplayItem.GenerateType();
					gameplayItem.GenerateSize();
				}

				(gameplayItem as UIElement).SetValue(Canvas.TopProperty, (double)gameplayItem.CoordY);
				(gameplayItem as UIElement).SetValue(Canvas.LeftProperty, (gameplayItem.CoordX - straight_counter * StarSpeedModifier) % (Width));
			}

		}







		/// <summary>
		/// Updates explosions
		/// Enemy canvas updater
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		public void EnemyUpdater_Tick(object sender, EventArgs e)
		{
			foreach(IGameItem bullet in EnemyCanvas.Children)
			{
				if(bullet is Enemy)
					(bullet as Enemy).GenerateType();

			}


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


		/// <summary>
		/// Updates bullets
		/// Enemy canvas updater
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		public void PlayerShootingUpdater_Tick(object sender, EventArgs e)
		{

			foreach(IGameItem bullet in EnemyCanvas.Children)
			{
				if(bullet is Bullet)
				{
					if(bullet.CoordX < Width)
						(bullet as Bullet).GoForward();

				}

			}

			if(BulletRemoveIterator < EnemyCanvas.Children.Count)
			{
				var t = EnemyCanvas.Children[BulletRemoveIterator] as Bullet;
				//if(t.CoordX > Width)											// WAS
				//	EnemyCanvas.Children.RemoveAt(BulletRemoveIterator);

				if(t?.CoordX > 500)
				{
					var v = new Explosion(EnemyCanvas.Children[BulletRemoveIterator],Height);


					ExplosionCanvas.Children.Add(v);

					EnemyCanvas.Children.RemoveAt(BulletRemoveIterator);
				}

				BulletRemoveIterator++;
			}
			else
				BulletRemoveIterator = 0;

		}


		/// <summary>
		/// Updates speed TODO : (and UI)
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		public void PlayerShipUpdater_Tick(object sender, EventArgs e)
		{
			CurrentPlayer.GenerateType();

			#region Counters

			straight_counter++;
			Counter++;

			topScore = Counter > topScore ? Counter : topScore;

			#endregion

			#region SPEED

			// TODO view model

			speed.Background = new SolidColorBrush(Colors.Transparent);
			speed.Margin = new Thickness(5, 35, 0, 0);
			speed.FontSize = 20.0;
			speed.Foreground = new SolidColorBrush(Colors.White);
			speed.Text = "SPEED: " + CurrentPlayer.CurrentSpeed.ToString() + "  ";
	
			#endregion

			#region Startup flicker

			if(isStartupFlicker)
			{
				if(Counter > 30 || (Counter < 30 && Counter % 5 < 3))
					PlayerCanvas.Children.Add(CurrentPlayer);		// fix need to be removed
			}

			#endregion

			#region Obstacle updating

			if(isObstacleEnabled)
				foreach(ObstacleFlapppy obstacle in ObstactleList)
				{
					double ob_gap = ob_GapBase * obstacle.Left / Width + ob_GapEnd;
					double top_height = (Height - ob_gap) * Math.Pow(Math.Sin((obstacle.Height + obstacle.Neg * 2 * obstacle.Left / Width)), 2.0);

					Color color = obstacle.IsHit ? Colors.Red : Colors.SlateGray;

					Rectangle top = new Rectangle()
					{
						Width = ob_Width,
						Height = top_height,
						Stroke = new SolidColorBrush(Colors.White),
						StrokeThickness = 0,
						Fill = new SolidColorBrush(color)
					};
					top.SetValue(Canvas.TopProperty, 0.0);
					top.SetValue(Canvas.LeftProperty, obstacle.Left);

					Rectangle bottom = new Rectangle()
					{
						Width = ob_Width,
						Height = Height - top_height - ob_gap,
						Stroke = new SolidColorBrush(Colors.White),
						StrokeThickness = 0,
						Fill = new SolidColorBrush(color)
					};
					bottom.SetValue(Canvas.TopProperty, top_height + ob_gap);
					bottom.SetValue(Canvas.LeftProperty, obstacle.Left);

					obstacle.VisualRect_top = top;
					obstacle.VisualRect_bottom = bottom;
					PlayerCanvas.Children.Add(top);
					PlayerCanvas.Children.Add(bottom);

					obstacle.Left -= ob_Speed;

					if(obstacle.Left + ob_Width < 0.0)
					{
						obstacle.Left = Width;
						obstacle.Height = r.NextDouble();
						obstacle.Neg = (r.Next() % 2) * 2 - 1;
						obstacle.IsHit = false;
					}


					// TODO -- fix collision detection

					//////////// collision detection 
					//////////if(!obstacle.IsHit && IsCollision(CurrentPlayer, obstacle.VisualRect_top) || IsCollision(CurrentPlayer, obstacle.VisualRect_bottom))
					//////////{
					//////////	obstacle.IsHit = true;
					//////////	isNewGame = false;

					//////////	GameOverEvent(this, EventArgs.Empty);

					//////////	GameplayTimer.Stop();
					//////////	return;
					//////////}
				}

			#endregion

		}


		/// <summary>
		/// Updates movement of player
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		public void UserMovement_Tick(object sender, EventArgs e)
		{
			if(isMovingForward && !CurrentPlayer.IsSpeedMaximum())
			{
				if(isMovingBackward)
					isMovingBackward = false;

				int f = (int)(CurrentPlayer.CurrentSpeed + CurrentPlayer.MaximumSpeed -
					CurrentPlayer.MaximumSpeed * Math.Exp(-((ForwardIterator += 0.5)) * CurrentPlayer.ForwardSpeedModifier));

				if(f < CurrentPlayer.MaximumSpeed)
					CurrentPlayer.CurrentSpeed = f;

			}

			if(isMovingBackward && !isMovingForward)
			{
				int v = (int)(CurrentPlayer.CurrentSpeed * Math.Exp(-(BackwardIterator += 0.5) * CurrentPlayer.BackwardSpeedModifier));

				if(CurrentPlayer.IsSpeedMinimum())
				{
					CurrentPlayer.CurrentSpeed = CurrentPlayer.MinimumSpeed;
					isMovingBackward = false;
				}

				CurrentPlayer.CurrentSpeed = v;
			}

			if(isMovingUpward)
			{
				int f = (int)(CurrentPlayer.CurrentLift + 4 * Math.Exp(-((UpwardIterator += 0.5)) * 0.2));

				if(f < Height - 10)
					CurrentPlayer.CurrentLift = f;
			}

			if(isMovingDownward)
			{
				int f = (int)(CurrentPlayer.CurrentLift - 2 * Math.Exp(-((DownwardIterator -= 0.5)) * 0.2));

				if(f > 120)
					CurrentPlayer.CurrentLift = f;
				
			}

			UpdatePlayerPosition();

		}















		private void Window_KeyDown(object sender, KeyEventArgs e)
		{
			if(e.Key == Key.Right)
				isMovingForward = true;	

			if(e.Key == Key.Up)
			{
				isMovingUpward = true;
				UpwardIterator = 0;
			}

			if(e.Key == Key.Down)
			{
				isMovingDownward = true;
				DownwardIterator = 0;
			}


			if(e.Key == Key.Space)
				CurrentPlayer.MakeAShot(EnemyCanvas);

			if(isNewGame)
				TryStartNewGame();


		}


		private void Window_KeyUp(object sender, KeyEventArgs e)
		{

			if(e.Key == Key.Right)
			{
				isMovingBackward = true;
				isMovingForward = false;
				BackwardIterator = 0;
				ForwardIterator = 0;
			}


			if(e.Key == Key.Up)
				isMovingUpward = false;

			if(e.Key == Key.Down)
				isMovingDownward = false;


		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Window_MouseDown(object sender, MouseButtonEventArgs e)
		{
			TryStartNewGame();
		}




		private void TryStartNewGame()
		{
			if(isNewGame)
			{
				InitializeCanvas();
				ResetAll();

				GameplayTimer.Start();  // DONT TOUCH THE	
				
				isNewGame = false;
			}

		}


		#endregion











	}

}
