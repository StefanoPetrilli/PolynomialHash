# PolynomialHash

.NET 10 Polynomial Rolling Hash Library which lets you compute a polynomial rolling hash over any `IEnumerable<T>` sequence with a single extension method call.

It features a **Zero-Cost Seamless API** that automatically uses AVX-512 or AVX2 SIMD acceleration for numeric types, achieving hardware-limit performance by eliminating delegate overhead.

## Examples

### Hashing sequences (Seamless API)

For numeric types (`int`, `long`, `char`, etc.), the library automatically maps values without requiring a selector.

```csharp
using PolynomialHash;

// Seamlessly hash a string (uses AVX-512/AVX2 automatically)
ulong hash = "hello world".ToPolynomialHash();

// Seamlessly hash a list of integers
int hash32 = new[] { 1, 2, 3, 4, 5 }.ToInt32PolynomialHash();

// Hash a sequence of custom objects by a meaningful key
var orders = new[] { new Order(id: 1, amount: 99), new Order(id: 2, amount: 42) };
ulong orderHash = orders.ToPolynomialHash(o => o.Id);
```

### Using as an equality comparer in collections

`PolynomialHasher<T>` implements `IEqualityComparer<IEnumerable<T>>`, allowing you to use sequences as keys in `HashSet` or `Dictionary`.

```csharp
using PolynomialHash;

// No selector needed for numeric types!
var comparer = new PolynomialHasher<int>();

// HashSet that deduplicates sequences by their contents
var seen = new HashSet<IEnumerable<int>>(comparer);
seen.Add(new[] { 1, 2, 3 }); // added
seen.Add(new[] { 1, 2, 3 }); // duplicate skipped
```

### Performance

The library is optimized for .NET 10 and leverages modern CPU features:

- **AVX-512 & AVX2**: Processes 8 (AVX-512) or 4 (AVX2) elements in parallel.
- **Zero-Cost Mapping**: Uses generic specialization to inline mapping logic, achieving up to **2x faster throughput** than standard delegate-based hashing.
- **Zero Allocations**: Operates entirely on the stack and registers for `ReadOnlySpan<T>` inputs.

### Tuning the hash

All methods accept optional `prime` and `mod` parameters:

```csharp
ulong hash = mySequence.ToPolynomialHash(
    prime: 131,
    mod: 1_000_000_007);
```
