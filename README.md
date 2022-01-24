# GetDataFromOBDIIBluetoothDevice
If you have idea where you have to get some information from your vehicle, like speed, rotation per minute, air temperature, fuel consumption or something else then this ode will help you a lot.

First of all, for using this app, you have to use also some of the OBDII bluetooth devices.
After you coonect this device to your vehicle, you have to pair your device with OBDII device, after that you'll be able to use app and receive information from your vehicle.

Every vehicle will send you information when you send some code to it. For example if you send code: 010C\r yoou'll receive vehicle speed information.
You'll receive this information in form of hexadecimal number. That's why this app is full of different methods that converts hexadecimal to decimal.

Also, information that you receive back will be in many rows and with many signs, that's why every received information firstly go through many methods that "clean" information and then present it to user.

You can use this code for making many apps that will be used for:
- Tracking driver's behaviour and way of driving
- Fixing car's mistakes
- Apps for comparing cars behaviour, etc.

There are many different OBDII devices, many of them works only on cars, so for testing this app on trucks you'll probably need special OBDII device.

This is my final presentation, but I used many other GitHub repositories that helped me to get this fnal app.
