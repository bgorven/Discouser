discouser
=========

Windows client for discourse.

Not in a usable state -- this is more of an exploratory project for me;
learning how to write an app and getting familiar with the discourse api.

I rolled my own mvvm framework. Basically the idea is that each viewmodel
has a `NotifyChanges()` method, a `LoadChanges()` method and a `Changes`
property. I hate when apps reaload the ui when I'm reading, so my plan
is that the background task polling for updates will notify each viewmodel
when there are changes, the viewmodel will pre-load those changes, and set
the changes property true, which will enable a button in the view that will
be bound to `LoadChanges`, which will trigger the ui update.

All ui data comes from the (sqlite.net) database. It is the DataContext's
(in `Discouser.Shared/Data`) responsibility to keep online content in sync
with the database, and to let viewmodels know when it is time to reload 
their data.
