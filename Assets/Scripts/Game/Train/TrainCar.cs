namespace Game.Train
{
	public class TrainCar : TrainBase
	{
		private float _brake;
		public override float BrakeForce => _brake;

		
		public void SetBreakForce(float value)
		{
			_brake = value;
			foreach (Bogie bogie in Bogies)
			{
				bogie.SetBrakeForce(_brake);
			}
		}
	}
}