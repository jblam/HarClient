# HarClient

TL;DR: Produce HAR from `HttpClient` in .NET

As at 2020-06-20, this project is extremely immature and not ready for any kind of use.

## What

HAR is an archive format for HTTP traffic.

HAR [is specified by the creator](http://www.softwareishard.com/blog/har-12-spec/); it appears the W3C were planning
to standardise it but decided not to, for whatever reason.

> *DO NOT USE*  
> This document was never published by the W3C Web Performance Working GRoup and has been abandonned.

-- [W3C](https://w3c.github.io/web-performance/specs/HAR/Overview.html); typos *sic*

This project treats the creator's [online viewer](http://www.softwareishard.com/har/viewer/) as the authoritative
validator.

## Why

Seems there is no "dead-simple" way of logging your application's HTTP traffic in .NET. Let's make one!

## How

`HttpMessageHandler` is the intended means of injecting your own code into an HTTP request pipeline.
We aim to create an implementation of `HttpMessageHandler` which will log all through traffic, then
produce a HAR on demand.

## Limitations

It appears there are lots of HAR features that cannot be produced:

- pages doesn't exist as a concept
- timings breakdown, e.g. DNS, Connection, may not be visible to .NET client code
- connection identifiers may not be visible to .NET client code
- remote IP address may not be visible to .NET client code
- detail of the HTTP traffic "on the wire" is not visible to .NET client code
  (for example, the raw HTTP headers)

Further, there may be unavoidable performance and behaviour issues when using something like this:

- recording and serialising data as it passes through, without impacting performance of the consumer,
  may be a challenge
- the HAR message handler should be totally transparent, which may require reimplementing certain behaviour
  of the lower-level handlers
  
However, there is some value (at least to the author!) in producing a simple log of incoming and outgoing request data.
