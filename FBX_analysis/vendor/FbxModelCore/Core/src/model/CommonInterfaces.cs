using readOnly;

namespace fin.model;

[GenerateReadOnly]
public partial interface INamed {
  new string? Name { get; set; }
}