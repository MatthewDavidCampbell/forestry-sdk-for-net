# Overview
Raindrop is an structured identity provider inspired by Twitter's [Snowflake](https://github.com/jtejido/snowflake) where the identity is composed of:

  - prefix
  - time where the lifetime is based on identity profiles
  - creation rate
  - node id

  Usage is based on profiles where the lifetime can be static in relation to the number of nodes in a system plus the creation rate.  Each region in the test cases should give a pretty good picture of how identities are created.  The takeaway is that no external resources are needed to secure unique identities.