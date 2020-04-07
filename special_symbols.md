# `vftable`

The virtual function table. Collection of addresses for the final overload of all the virtual functions within a class.

If there is multiple inheritance, and there is a need to generate a vftable for each of the bases, there there will be a "for" part in this symbol, naming each of the bases needed.

# `vbtable`

The virtual base table, contains informaion for virtual bases.

If there is multiple inheritance, the vbtable may be generated for each of the virtual base. It will be demangled with a for part in the "vbtable".

# `vcall`

It is a fucntion (thunk) generated when getting a pointer to virtual member function.

In C++, the final overrider is required to be called when the function is virtual. So, pointer to virtual member function is different from pointer to non-virtual member function, as it must find the final overrider first. A `vcall` thunk is generated to do this.

The number part in the `vcall` output seems to be an offset in the vftable. The meaning of the `flat` part is unknown, and no other alternative has been seen.