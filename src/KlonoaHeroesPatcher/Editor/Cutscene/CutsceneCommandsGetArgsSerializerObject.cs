using System;
using System.Collections.Generic;
using System.Text;
using BinarySerializer;
using BinarySerializer.Klonoa.KH;

namespace KlonoaHeroesPatcher;

/// <summary>
/// A serializer object to be used for <see cref="CutsceneCommand"/> to retrieve the arguments
/// </summary>
public class CutsceneCommandsGetArgsSerializerObject : SerializerObject
{
    public CutsceneCommandsGetArgsSerializerObject(Context context) : base(context)
    {
        Arguments = new List<CommandArgument>();
    }

    public List<CommandArgument> Arguments { get; }

    public override long CurrentLength => 0;
    public override BinaryFile CurrentBinaryFile => null;
    public override long CurrentFileOffset => 0;

    public override void Log(string logString) { }

    public override Pointer BeginEncoded(IStreamEncoder encoder, Endian? endianness = null, bool allowLocalPointers = false,
        string filename = null) => throw new InvalidOperationException();
    public override void EndEncoded(Pointer endPointer) => throw new InvalidOperationException();

    public override void DoEncoded(IStreamEncoder encoder, Action action, Endian? endianness = null, bool allowLocalPointers = false,
        string filename = null) => throw new InvalidOperationException();

    public override void Goto(Pointer offset) => throw new InvalidOperationException();

    public override T SerializeChecksum<T>(T calculatedChecksum, string name = null) => throw new InvalidOperationException();

    public override T Serialize<T>(T obj, string name = null)
    {
        Arguments.Add(new CommandArgument(name, obj, typeof(T)));
        return obj;
    }

    public override T SerializeObject<T>(T obj, Action<T> onPreSerialize = null, string name = null) => throw new InvalidOperationException();

    public override Pointer SerializePointer(Pointer obj, PointerSize size = PointerSize.Pointer32, Pointer anchor = null,
        bool allowInvalid = false, long? nullValue = null, string name = null) => throw new InvalidOperationException();

    public override Pointer<T> SerializePointer<T>(Pointer<T> obj, PointerSize size = PointerSize.Pointer32, Pointer anchor = null,
        bool resolve = false, Action<T> onPreSerialize = null, bool allowInvalid = false, long? nullValue = null,
        string name = null) => throw new InvalidOperationException();

    public override string SerializeString(string obj, long? length = null, Encoding encoding = null, string name = null) => throw new InvalidOperationException();

    public override T[] SerializeArraySize<T, U>(T[] obj, string name = null) => throw new InvalidOperationException();

    public override T[] SerializeArray<T>(T[] obj, long count, string name = null) => throw new InvalidOperationException();

    public override T[] SerializeObjectArray<T>(T[] obj, long count, Action<T, int> onPreSerialize = null, string name = null) => throw new InvalidOperationException();

    public override T[] SerializeArrayUntil<T>(T[] obj, Func<T, bool> conditionCheckFunc, Func<T> getLastObjFunc = null, string name = null) => throw new InvalidOperationException();

    public override T[] SerializeObjectArrayUntil<T>(T[] obj, Func<T, bool> conditionCheckFunc, Func<T> getLastObjFunc = null,
        Action<T, int> onPreSerialize = null, string name = null) => throw new InvalidOperationException();

    public override Pointer[] SerializePointerArray(Pointer[] obj, long count, PointerSize size = PointerSize.Pointer32,
        Pointer anchor = null, bool allowInvalid = false, long? nullValue = null, string name = null) =>
        throw new InvalidOperationException();

    public override Pointer<T>[] SerializePointerArray<T>(Pointer<T>[] obj, long count, PointerSize size = PointerSize.Pointer32,
        Pointer anchor = null, bool resolve = false, Action<T, int> onPreSerialize = null, bool allowInvalid = false,
        long? nullValue = null, string name = null) =>
        throw new InvalidOperationException();

    public override string[] SerializeStringArray(string[] obj, long count, int length, Encoding encoding = null, string name = null) => throw new InvalidOperationException();

    public override void DoEndian(Endian endianness, Action action) => throw new InvalidOperationException();

    public override T DoAt<T>(Pointer offset, Func<T> action) => default;

    public override void SerializeBitValues(Action<SerializeBits64> serializeFunc) => throw new InvalidOperationException();

    public override void DoBits<T>(Action<BitSerializerObject> serializeFunc)
    {
        serializeFunc(new CutsceneCommandsGetArgsBitSerializerObject(this, CurrentPointer));
    }

    public record CommandArgument(string Name, object Value, Type type);

    private class CutsceneCommandsGetArgsBitSerializerObject : BitSerializerObject
    {
        public CutsceneCommandsGetArgsBitSerializerObject(CutsceneCommandsGetArgsSerializerObject serializerObject, Pointer valueOffset) : base(serializerObject, valueOffset, default, default)
        { }

        public override T SerializeBits<T>(T value, int length, string name = null)
        {
            ((CutsceneCommandsGetArgsSerializerObject)SerializerObject).Arguments.Add(new CommandArgument(name, value, typeof(T)));
            return value;
        }
    }
}