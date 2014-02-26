TheFactory.Snappy
=================

A Snappy encoder/decoder for Xamarin / C#

This begins as an embarrassingly direct port of snappy-go:
https://code.google.com/p/snappy-go/

It's so direct that we've left the original license and copyright
intact.

It was created because the existing Snappy options for .NET either
weren't native (requiring tricky bindings & build for iOS and Android)
or had encoding bugs that proved annoying to fix.

The main goals of this project are correctness and maintainability: it
should produce the same bytes as the snappy-go encoder, and to that
end it's almost a line by line port of that code.

It passes the unit tests from snappy-go and works with our data that
failed with other encoders. It has round-tripped 512M of random bytes,
and we expect to do further testing at larger (especially for mobile)
scale.

Which is to say: it passes moderate tests and expects to see
production use. We hope you find it useful.
