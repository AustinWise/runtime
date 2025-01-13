// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.TypeSystem;
using Internal.IL.Stubs;

using Debug = System.Diagnostics.Debug;
using System;

namespace Internal.IL
{
    public class MonoInternalCallILProvider : ILProvider
    {
        public MonoInternalCallILProvider()
        {
        }

        public override MethodIL GetMethodIL(MethodDesc method)
        {
            TypeSystemContext context = method.Context;
            MetadataType lazyHelperType = context.GetHelperType("InteropHelpers");

            MethodSignature managedSig = method.Signature;
            TypeDesc nativeReturnType = managedSig.ReturnType;
            TypeDesc[] nativeParameterTypes = new TypeDesc[managedSig.Length];
            for (int i = 0; i < nativeParameterTypes.Length; i++)
            {
                nativeParameterTypes[i] = managedSig[i];
            }
            MethodSignature nativeSig = new MethodSignature(
                MethodSignatureFlags.Static | MethodSignatureFlags.UnmanagedCallingConvention, 0, nativeReturnType, nativeParameterTypes,
                method.GetInternalCallCallingConventions().EncodeAsEmbeddedSignatureData(context));

            ILEmitter emitter = new ILEmitter();
            var functionPointerType = method.Context.GetFunctionPointerType(method.Signature);
            var fnPtrLocal = emitter.NewLocal(functionPointerType);
            var stream = emitter.NewCodeStream();

            // TODO: use a fully qualified type name that matches Mono's behavior
            stream.Emit(ILOpcode.ldstr, emitter.NewToken(method.Name));
            stream.Emit(ILOpcode.call, emitter.NewToken(lazyHelperType
                    .GetKnownMethod("ResolveInternalCall", null)));
            stream.EmitStLoc(fnPtrLocal);

            for (int i = 0; i < nativeParameterTypes.Length; i++)
            {
                stream.EmitLdArg(i);
            }
            stream.EmitLdLoc(fnPtrLocal);
            stream.Emit(ILOpcode.calli, emitter.NewToken(nativeSig));
            stream.Emit(ILOpcode.ret);
            return emitter.Link(method);
        }
    }
}
