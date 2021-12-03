//-----------------------------------------------------------------------
// <copyright file="BluetoothLEScanOptions.windows.cs" company="In The Hand Ltd">
//   Copyright (c) 2018-21 In The Hand Ltd, All rights reserved.
//   This source code is licensed under the MIT License - see License.txt
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using Windows.Foundation;

namespace InTheHand.Bluetooth
{
    partial class BluetoothLEScanOptions
    {
        private readonly BluetoothLEAdvertisementFilter _platformFilter;

        public BluetoothLEScanOptions()
        {
            _platformFilter = new BluetoothLEAdvertisementFilter();
        }

        internal BluetoothLEScanOptions(BluetoothLEAdvertisementFilter filter)
        {
            _platformFilter = filter;
        }

        public static implicit operator BluetoothLEAdvertisementFilter(BluetoothLEScanOptions options)
        {
            if (!options.AcceptAllAdvertisements)
            {
                foreach (var filter in options.Filters)
                {
                    //options._platformFilter.Advertisement.LocalName = filter.Name;
                    options._platformFilter.Advertisement.ServiceUuids.Clear();
                    foreach (var service in filter.Services)
                    {
                        options._platformFilter.Advertisement.ServiceUuids.Add(service.Value);
                    }
                }
            }

            return options._platformFilter;
        }

        public static implicit operator BluetoothLEScanOptions(BluetoothLEAdvertisementFilter filter)
        {
            return new BluetoothLEScanOptions(filter);
        }

       
    }
}