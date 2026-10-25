using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenNFS1.Physics;

namespace OpenNFS1.Vehicles.AI
{
	class PlayerDriver : IDriver
	{
		DrivableVehicle _vehicle;

		public Vehicle Vehicle { get { return _vehicle; } }
		public PlayerDriver(DrivableVehicle vehicle)
		{
			_vehicle = vehicle;
		}

		public void Update(List<IDriver> otherDrivers)
		{
			var drive = ReverseDriveControl.Compute(GameConfig.ManualGearbox,
                _vehicle.Motor.Gearbox.CurrentGear, _vehicle.Speed,
                VehicleController.Acceleration, VehicleController.Brake,
                VehicleController.GearUp, VehicleController.GearDown);
            _vehicle.ThrottlePedalInput = drive.Throttle;
            _vehicle.BrakePedalInput = drive.Brake;
            _vehicle.SteeringInput = VehicleController.Turn;
            _vehicle.GearDownInput = drive.ShiftDown;
            _vehicle.GearUpInput = drive.ShiftUp;
			_vehicle.HandbrakeInput = VehicleController.Handbrake;
			_vehicle.Update();
		}

		public void Render()
		{

		}
	}
}
