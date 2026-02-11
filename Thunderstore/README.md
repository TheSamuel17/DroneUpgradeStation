# DroneUpgradeStation

The following changes are made:
* By default, Drone Upgrade Stations are allowed to spawn on every Stage 4 & 5, same as the Drone Combiner Station. Only one can spawn per stage.
    * A guaranteed one will also spawn on Computational Exchange.
* Now checks for the Mechanical body flag instead of Drone. This notably allows Gunner Turrets to be upgraded, but it also allows temporary Mechanical allies (e.g. Empathy Cores, The Back-up) to benefit from it.
* Blacklists every item with the CannotCopy tag, or in other words, anything that turrets can't inherit.
    * Extra items that are blacklisted by default: Rusted Key, Shipping Request Form, Bundle of Fireworks, Squid Polyp, Sale Star, Chance Doll, Hopoo Feather, Wax Quail, Substandard Duplicator, War Bonds, Functional Coupler.
* It's green.

Not much else has been done in terms of polish, this interactable has been brought back mostly as-is.

## To-do (maybe):

* Differentiate its model so it's not just a green'd-up Drone Combiner Station.
* Stop players from wasting items if they don't have anything to upgrade.
* SFX.

## Credits

* Captain Baconator, for throwing me into this sidequest.
* Whoever worked on LookingGlass's UI elements, for writing helpful and *definitely* yoinkable code.
* The denizens of `#development`, for answering my questions.

## Contact

Direct your feedback & bug reports to `samuel17` on Discord, either through DMs or the Risk of Rain 2 Modding Discord.