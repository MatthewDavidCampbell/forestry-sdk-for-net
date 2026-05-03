# Overview
 Crystialized water floating down as snow is like DNA but raindrops when splashing are decent thumbprints dependiing on how hard you press.  Raindrop is an structured identity provider inspired by Twitter's [Snowflake](https://github.com/jtejido/snowflake) where the identity is composed of:

  - prefix
  - time where the lifetime is based on identity profiles
  - creation rate
  - node id

  Usage is based on profiles where the lifetime can be static in relation to the number of nodes in a system plus the creation rate.  Each region in the test cases should give a pretty good picture of how identities are created.  The takeaway is that no external resources are needed to secure unique identities.

  A major **TODO** is the use of policies allowing for more customization.

  ## Node resolution
  There are test cases showing the resolution of Node Id options and example code for Service Fabric which has a cluster of nodes.

  **TODO** Kubernetes example using environment variables (replic count and mod of pod name).