I need to find all api actions called by the UI, where the data does not match the entity used for crud oerations, for example data is retrieved after joining multiple tables or a subset of data is requested.
Once identified we need to plan a refactor which implies:
1) removal of all methods created for the sole purpose of serving data to the UI
2) creation of new controllers with the rounting "odata"instead of "api", in a sub folder called UIControllers, these controllers should only depend on a readonly db context, all queries should be optimized for the sole purpose they are created, not meant to be reused, and highly optimized for performance. The read only controller should not track automatically and should not support save changes
So controllers serving the UI do not use minded, but CRUD controllers must.
3) refactor all tests affected
4) create unit and e2e tests to cover the new odata controllers
 