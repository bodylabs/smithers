Smithers
========

An extensible framework for using Kinect 2 in WPF


Features
--------

- Basic visualization for raw sensor data
- Visualization of skeleton data in 2D
- Projections for the color and depth/infrared images to draw in 3D
- An extensible structure for building your own behaviors
- Serialization of frame data in a compact, cross-platform format

The framework is in under active development and all APIs are subject
to change.


Installation
------------

1. Install [Visual Studio Express 2013 for Windows Desktop][vstudio].
2. Install the Kinect SDK.
3. Create a new repository and add smithers as a submodule:

        git submodule add vendor/smithers https://github.com/bodylabs/smithers.git

4. Create a new solution and add the four projects:

    - Smithers.Reading
    - Smithers.Visualization
    - Smithers.Serialization
    - Smithers.Sessions

5. Build the solution. The app will automatically install most of the
   remaining dependencies.
6. To compress session data using LZMA format, you need to get a copy
   of 7z.dll. See the documentation of [monocle][] for more information.


Contribute
----------

- Issue Tracker: github.com/bodylabs/smithers/issues
- Source Code: github.com/bodylabs/smithers

Pull requests welcome!


Support
-------

To get started with the framework, see the example capture application
[Monocle][].

If you are having issues, please let us know.


License
-------

The project is licensed under the two-clause BSD license.

Smithers uses 7z.dll, which is part of the 7-Zip program, licensed under the
GNU LGPL license. You can obtain the source code from www.7-zip.org.


[vstudio]: http://www.microsoft.com/en-us/download/details.aspx?id=40787
[monocle]: https://www.github.com/bodylabs/monocle
