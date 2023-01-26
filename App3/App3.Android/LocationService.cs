﻿using Android.App;
using Android.Content;
using Android.Locations;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Android.Support;




namespace App3.Droid
{
    [Service]
    public class LocationService : Service, ILocationListener
    {

        LocationManager _locationManager;
        string _locationProvider;

        const int NOTIFICATION_ID = 1;
        NotificationManager manager;

        public override IBinder OnBind(Intent intent)
        {
            return null;
        }

        public override void OnCreate()
        {

            base.OnCreate();

            manager = (NotificationManager)GetSystemService(NotificationService);
            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                var channel = new NotificationChannel("channel_01", "My Channel", NotificationImportance.High);
                manager.CreateNotificationChannel(channel);
            }
            else {
                Toast.MakeText(this, "Notifications not supported", ToastLength.Long).Show();
            }

            _locationManager = (LocationManager)GetSystemService(LocationService);
            _locationProvider = LocationManager.GpsProvider;

            if (_locationManager.IsProviderEnabled(_locationProvider))
            {
                _locationManager.RequestLocationUpdates(_locationProvider, 0, 0, this);
            }
            else
            {
                Toast.MakeText(this, "Please enable GPS.", ToastLength.Long).Show();
            }


        }
        

        public override StartCommandResult OnStartCommand(Intent intent, StartCommandFlags flags, int startId) {

            Android.Util.Log.Debug("LocationService", "OnStartCommand Called");


            // manager.Notify(NOTIFICATION_ID, notification);





            return StartCommandResult.Sticky;

        }
      
        public override void OnDestroy()
        {
            Log.Debug("LocationService", "StopService called");
            base.OnDestroy();
            _locationManager.RemoveUpdates(this);

        }

        public void OnLocationChanged(Location location)
        {
            // Do something with the location here
        }

        public void OnProviderDisabled(string provider) { }

        public void OnProviderEnabled(string provider) { }

        public void OnStatusChanged(string provider, [GeneratedEnum] Availability status, Bundle extras) { }
    }

}