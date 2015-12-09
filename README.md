Kvant/Swarm v2
==============

*Swarm* is a specialized renderer/animator that draws organic flowing lines with
a physically based shader. It fully utilizes GPU to process flowing animation
and mesh deformation, and thus it can draw very large number of lines without
consuming precious CPU time.

![gif](http://45.media.tumblr.com/65956e640f88d08da86ae7b238eb9889/tumblr_nz32r08PoG1qio469o1_400.gif)
![gif](http://49.media.tumblr.com/6d2ddb78263867686bd1f7e9934eaf42/tumblr_nz32r08PoG1qio469o2_400.gif)

![gif](http://49.media.tumblr.com/adb02e99da464a69137c407f2b9a9cff/tumblr_nz32rxg18E1qio469o1_400.gif)
![gif](http://49.media.tumblr.com/b60a64d4815d6bda1221afbf4598be92/tumblr_nz32rxg18E1qio469o2_400.gif)

*Swarm* is part of the *Kvant* effect suite. Please see the [GitHub
repositories][kvant] for further information about the suite.

[kvant]: https://github.com/search?q=kvant+user%3Akeijiro&type=Repositories

System Requirements
-------------------

Unity 5.1 or later versions.

*Kvant* effects require floating-point HDR textures to store animation state.
Most of mobile devices don't fulfill this requirement at the moment.

No Backward Compatibility
-------------------------

This version (v2) is not compatible with the previous versions. You can't simply
upgrade the previous implementation or use two different versions in the same
project. Sorry for the inconvenience!

License
-------

Copyright (C) 2015 Keijiro Takahashi

Permission is hereby granted, free of charge, to any person obtaining a copy of
this software and associated documentation files (the "Software"), to deal in
the Software without restriction, including without limitation the rights to
use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of
the Software, and to permit persons to whom the Software is furnished to do so,
subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS
FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR
COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER
IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
