# PolynomialHash
.NET10 Polynomial Rolling Hash Library which lets you compute a polynomial rolling hash over any `IEnumerable<T>` sequence with a single extension method call. It is also built for native integration, allowing you to plug it directly into standard data structures like `Dictionary` or `HashSet` to easily treat sequences as keys.

## Examples

### Hashing sequences

```csharp
using PolynomialHash;

// Hash a string using the numeric value of each character
ulong hash = "hello world".ToUInt64PolynomialHash(c => c);

// Hash a list of integers
int hash32 = new[] { 1, 2, 3, 4, 5 }.ToInt32PolynomialHash(x => x);

// Hash a sequence of custom objects by a meaningful key
var orders = new[] { new Order(id: 1, amount: 99), new Order(id: 2, amount: 42) };
ulong orderHash = orders.ToUInt64PolynomialHash(o => o.Id);
```

### Using as an equality comparer in collections

`PolynomialHasher<T>` implements `IEqualityComparer<IEnumerable<T>>`, which means you can plug it directly into a `HashSet` or `Dictionary` to treat sequences as keys. Two sequences with the same elements in the same order will be considered equal.

```csharp
using PolynomialHash;

var comparer = new PolynomialHasher<int>(x => x);

// HashSet that deduplicates sequences by their contents
var seen = new HashSet<IEnumerable<int>>(comparer);
seen.Add(new[] { 1, 2, 3 }); // added
seen.Add(new[] { 4, 5, 6 }); // added
seen.Add(new[] { 1, 2, 3 }); // duplicate
Console.WriteLine(seen.Count); // 2

// Dictionary keyed by integer sequences
var cache = new Dictionary<IEnumerable<int>, string>(comparer);
cache[new[] { 10, 20, 30 }] = "first entry";
Console.WriteLine(cache[new[] { 10, 20, 30 }]); // "first entry"
```

### Tuning the hash

All methods accept optional `prime` and `mod` parameters so you can adapt the hash to your performance or collision-resistance requirements:

```csharp
ulong hash = mySequence.ToUInt64PolynomialHash(
    valueSelector: x => x,
    prime: 131,
    mod: 1_000_000_007);
```
