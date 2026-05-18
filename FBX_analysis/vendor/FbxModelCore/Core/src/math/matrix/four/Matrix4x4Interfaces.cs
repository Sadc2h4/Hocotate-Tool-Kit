using System.Numerics;

using readOnly;

namespace fin.math.matrix.four;

[GenerateReadOnly]
public partial interface IFinMatrix4x4
    : IFinMatrix<IFinMatrix4x4, IReadOnlyFinMatrix4x4, Matrix4x4> {
IFinMatrix4x4 TransposeInPlace();

[Const]
new IFinMatrix4x4 CloneAndTranspose();

[Const]
new void TransposeIntoBuffer(IFinMatrix4x4 buffer);

[Const]
new void CopyTranslationInto(out Vector3 dst);

[Const]
new void CopyRotationInto(out Quaternion dst);

[Const]
new void CopyScaleInto(out Vector3 dst);

[Const]
new void Decompose(out Vector3 translation,
                   out Quaternion rotation,
                   out Vector3 scale);
}