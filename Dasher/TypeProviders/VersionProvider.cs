#region License
//
// Dasher
//
// Copyright 2015-2016 Drew Noakes
//
//    Licensed under the Apache License, Version 2.0 (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
//
//        http://www.apache.org/licenses/LICENSE-2.0
//
//    Unless required by applicable law or agreed to in writing, software
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and
//    limitations under the License.
//
// More information about this project is available at:
//
//    https://github.com/drewnoakes/dasher
//
#endregion

using System;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace Dasher.TypeProviders
{
    internal sealed class VersionProvider : ITypeProvider
    {
        public bool CanProvide(Type type) => type == typeof(Version);

        public bool TryEmitSerialiseCode(ILGenerator ilg, ICollection<string> errors, LocalBuilder value, LocalBuilder packer, LocalBuilder contextLocal, DasherContext context)
        {
            // write the string form
            ilg.Emit(OpCodes.Ldloc, packer);
            ilg.Emit(OpCodes.Ldloc, value);
            ilg.Emit(OpCodes.Call, Methods.Version_ToString);
            ilg.Emit(OpCodes.Call, Methods.UnsafePacker_Pack_String);

            return true;
        }

        public bool TryEmitDeserialiseCode(ILGenerator ilg, ICollection<string> errors, string name, Type targetType, LocalBuilder value, LocalBuilder unpacker, LocalBuilder contextLocal, DasherContext context, UnexpectedFieldBehaviour unexpectedFieldBehaviour)
        {
            // read value as string
            var s = ilg.DeclareLocal(typeof(string));

            ilg.Emit(OpCodes.Ldloc, unpacker);
            ilg.Emit(OpCodes.Ldloca, s);
            ilg.Emit(OpCodes.Call, Methods.Unpacker_TryReadString);

            // If the unpacker method failed (returned false), throw
            var lbl = ilg.DefineLabel();
            ilg.Emit(OpCodes.Brtrue, lbl);
            {
                ilg.Emit(OpCodes.Ldstr, $"Expecting string value for Version property {name}");
                ilg.LoadType(targetType);
                ilg.Emit(OpCodes.Newobj, Methods.DeserialisationException_Ctor_String_Type);
                ilg.Emit(OpCodes.Throw);
            }
            ilg.MarkLabel(lbl);

            ilg.Emit(OpCodes.Ldloc, s);
            ilg.Emit(OpCodes.Newobj, typeof(Version).GetConstructor(new[] {typeof(string)}));
            ilg.Emit(OpCodes.Stloc, value);

            return true;
        }
    }
}