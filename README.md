OTOMapper
=========

Object to object mapper for Windows Phone 8.

Useful to map similar structured objects with different application fields
Example: Domain Entity to UI Entity

This includes *four scenarios* and are executed in order of findings:

* Optional Custom Mappings specified at instantiation time
* Mapping exact property names: Property = Property
* Mapping source child property to destination property: Child.Property = ChildProperty
* Mapping source method to destination property: GetProperty() = Property

##Usage example

`OtoMapper.MapSingleEntities(entityToMapFrom, entityToMapInto)`

###With custom mappings:

`OtoMapper.MapSingleEntities(entityToMapFrom, entityToMapInto, customMappingsDictionary)`

A custom mapping dictionary must contain the destination property name as its key and the source property name as its value. Custom mappings only support Property to Property mappings.