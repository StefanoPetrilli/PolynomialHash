# AVX-512 Mastery: Building a High-Performance Polynomial Hash in .NET 10

Welcome! This guide is designed to take you from "Zero to Hero" in AVX-512 using .NET 10. By the end, you will have implemented a SIMD-accelerated version of your `ComputeHashFast` method.

---

## Phase 1: The Architectural Mental Model

### The ZMM Registers
In standard x64 programming, you work with 64-bit general-purpose registers (`rax`, `rbx`, etc.). In AVX-512, you unlock **ZMM registers**.
- **Width**: 512 bits (64 bytes).
- **Capacity**: It can hold exactly **8** `ulong` (64-bit) values simultaneously.
- **Number**: There are 32 of these registers (`zmm0` through `zmm31`).

### The "Eight-Lane Highway"
Think of your sequential code as a single cyclist. AVX-512 is an 8-lane highway where 8 cyclists move in perfect synchronization. 

**Important:** In .NET 10, we use the `Vector512<T>` type. This is a "hardware-intrinsic" type, meaning the JIT compiler maps it directly to a physical `zmm` register.

### 🧠 Concept Check: Parallelizing the Polynomial
Your sequential formula:
$H = (v_0 \cdot p^0) + (v_1 \cdot p^1) + (v_2 \cdot p^2) + \dots + (v_n \cdot p^n) \pmod{2^{64}}$

To use 8 lanes, we process elements in blocks of 8. 
- **Block 0**: Indices $[0, 1, 2, 3, 4, 5, 6, 7]$
- **Block 1**: Indices $[8, 9, 10, 11, 12, 13, 14, 15]$

Inside your SIMD code, you maintain 8 parallel partial sums:
- `SumVector[0]` = $v_0 p^0 + v_8 p^8 + v_{16} p^{16} \dots$
- `SumVector[1]` = $v_1 p^1 + v_9 p^9 + v_{17} p^{17} \dots$

### 📝 Exercise 1: The Multiplier Jump
In the sequential version, you multiply `primePower` by `p` every step. In the 8-lane SIMD version, after you process a block of 8, what do you need to multiply your "Powers Vector" by to prepare it for the *next* block?
*(Hint: How many "steps" forward does each lane jump?)*

---

## Phase 2: The .NET 10 Intrinsic Environment

### Hardware Detection (The Safety Net)
AVX-512 is powerful but not universal. Your code **must** have a fallback.

```csharp
if (Vector512.IsHardwareAccelerated && Avx512F.IsSupported)
{
    // Hero Path: AVX-512
}
else if (Vector256.IsHardwareAccelerated && Avx2.IsSupported)
{
    // Intermediate Path: AVX2 (4 lanes)
}
else
{
    // Human Path: Sequential
}
```

### Why two checks?
- `Vector512.IsHardwareAccelerated`: Tells you the .NET runtime can handle 512-bit types.
- `Avx512F.IsSupported`: Confirms the underlying CPU actually supports the "Foundation" (F) instruction set of AVX-512.

---

## Phase 3: High-Speed Data Loading

To feed the SIMD registers, we need to read from memory. `IEnumerable<T>` is too slow for this; we need contiguous memory (Arrays or Spans).

### The Instruction: `Vector512.LoadUnsafe`
This is the fastest way to load data. It takes a reference to a memory location and "slurps" up 512 bits.

### 📝 Exercise 2: Pointer Arithmetic
Given `ReadOnlySpan<long> data` and an index `i`, we use `Unsafe` to avoid the overhead of bounds checking:

```csharp
ref long sourceRef = ref MemoryMarshal.GetReference(data);
ref long currentPos = ref Unsafe.Add(ref sourceRef, i);
Vector512<ulong> block = Vector512.LoadUnsafe(ref currentPos).AsUInt64();
```
**Question**: If `i` is 8, how many **bytes** did `Unsafe.Add` move the pointer forward? (Recall that `long` is 8 bytes).

---

## Phase 4: Precomputing the Powers Vector

Before entering the loop, you must initialize your "Multipliers."

**Goal**: Create a `Vector512<ulong>` containing $[p^0, p^1, p^2, p^3, p^4, p^5, p^6, p^7] \pmod{2^{64}}$.

### .NET 10 Approach:
```csharp
ulong[] powers = new ulong[8];
ulong current = 1;
for(int j=0; j<8; j++) {
    powers[j] = current;
    current *= p;
}
Vector512<ulong> powersVec = Vector512.Create(powers);
```

---

## Phase 5: The "Hero" Loop

The main loop is where the magic happens. You should process `length - (length % 8)` elements here.

### The Algorithm:
1. **Load**: `Vector512<ulong> dataVec = Vector512.LoadUnsafe(...)`
2. **Multiply**: `Vector512<ulong> productVec = dataVec * powersVec;`
3. **Accumulate**: `accVec += productVec;`
4. **Update Powers**: `powersVec *= jumpVec;` (where `jumpVec` is a vector filled with $p^8$).

### 💡 Advanced Detail: `unchecked`
In C#, `ulong` multiplications inside `unchecked` blocks (which is the default for SIMD operators) perform **Modular Multiplication** automatically. This is why the "Fast Path" ($2^{64}$) is so perfect for SIMD.

---

## Phase 6: Horizontal Reduction

After the loop, `accVec` contains 8 values. You need their sum.

### The Instruction: `Vector512.Sum<ulong>(accVec)`
In older versions of .NET, you had to manually "shuffle and add" (extract high 256 bits, add to low 256, etc.). .NET 10 provides `Vector512.Sum<T>()`, which handles this horizontal reduction optimally for your hardware.

---

## Phase 7: Handling the "Tail" (The Heroic Way)

If your data size isn't a multiple of 8 (e.g., 11 elements), you have a "tail" of 3 elements.

### Level 1 (The Apprentice): 
Use a standard `for` loop for the remaining 3 elements. Start your `primePower` where the SIMD loop left off.

### Level 2 (The Hero): Masked Loading
AVX-512 introduced **Opmask Registers** (`k0`-`k7`). This allows you to load *partial* vectors.
- You can create a mask where only the first 3 bits are `1`.
- Use `Avx512F.LoadMasked(...)` to load only the 3 remaining elements into a vector, padding the rest with zeros.
- This allows you to use the **exact same SIMD logic** for the tail, with zero branching!

---

## 🛠 Final Implementation Plan

1.  **Preparation**: Ensure your input `IEnumerable<T>` is converted to a `Span<T>` or `Array`.
2.  **Constants**: Precompute $p^0 \dots p^7$ and a vector of $p^8$.
3.  **The SIMD Loop**: Process 8 elements per iteration.
4.  **Reduction**: Sum the lanes.
5.  **The Tail**: Handle remaining elements.
6.  **Verification**: Ensure your SIMD result exactly matches the sequential result for all input sizes.

### 📝 Final Exercise: Scaling
If you wanted to use **AVX2** (256-bit), how many lanes would you have, and what would your "jump" multiplier be?

---
**Next Step**: Try to implement the hardware detection and the "Powers Vector" initialization in your `PolynomialHasher.cs`.
