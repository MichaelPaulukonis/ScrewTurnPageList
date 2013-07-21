ScrewTurnPageList
=================

This is a simple plugin for [ScrewTurnWiki](https://stw.codeplex.com/) to provide a list of pages (titles)
based on one or more namespaces.

I wrote it about 2 years ago, and I don't have a full recall of the internals. Tsk tsk tsk.

However, I am using it in a wiki on a daily basis, so I know it is functioning in general.

The committed code has not been pulled and tested for compile as of 2013.07.21
There are some known hard-coded paths in the solution from my dev-system.


Markup
------

`{pagelist options}` where options are "namespace=(namespace)" "include=(namespace)" and "exclude=(namespace)" 
where the value on the right is a valid ScrewTurn namespace.
