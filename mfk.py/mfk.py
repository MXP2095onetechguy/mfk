#!/usr/bin/env python3

# Official MFK library and interpreter.
# MFK is a language based on FRACTRAN that is nicer to write in.
# See the accompanying mfk.ebnf for the syntax, if it is there.

import typing as tp
import functools as fnt
import io
import dataclasses as dc
import collections as col
import math

__all__ = [
    "churn"
]

## Types

class QueryableParent():
    "Parent class that does nothing except for a base"

@dc.dataclass(frozen=True,slots=True)
class InputRequest(QueryableParent):
    """
    Placeholder value for InputRequest
    """
    prompt : tp.Optional[str]
@dc.dataclass(frozen=True,slots=True)
class Random(QueryableParent):
    """
    Placeholder value for Random
    """
    min : int
    max : int

Queryable : tp.TypeAlias = int | InputRequest | Random
UNBOUNDED : Queryable = -1

@dc.dataclass
class CompiledMFK():
    initialN : Queryable
    maxIter : Queryable


class MFKError(Exception): "Base for all MFK Errors."

class MFKEOFError(MFKError, EOFError): """EOF Error for MFK."""

class MFKQueryableError(MFKError):
    """Thrown if there is a problem with the Queryable."""
    def __init__(self, message, letter : tp.Optional[str], errant : tp.Optional[str], **args):
        super().__init__(message, *args)
        self.letter = letter
        self.errant = errant
class MFKParametricError(MFKError):
    """Thrown if there's an error with the parametrics."""
    def __init__(self, message, suberror : MFKError, *args):
        super().__init__(message, *args)
        self.suberror = suberror

class MFKInputRequestError(MFKError):
    """Thrown if the Input Request is malformed."""

## INTERPRETER


## CHURN

@fnt.singledispatch
def churn(source) -> CompiledMFK:
    """
    Validates and compiles source into a bytecode format.
    """
    raise TypeError(f"Nuh uh. Only strings or streams allowed. {type(source)}")

def _queryableParse(source : io.TextIOBase, START, tellStack : col.deque) -> Queryable:
    val = source.read(1) # Read it
    if val == "": # Unexpected EOF?
        raise MFKEOFError("Unexpected EOF when value expected!")
    elif val.isdigit(): # Is it a number?
        digits = "" + val
        for char in iter(lambda : source.read(1), ""): # Iterate over stream

            if val == "0" and char.isdigit(): # 0 before other digits?
                raise MFKQueryableError("Leading 0s found!", None, char)
            
            if char == ",": # Is it a comma?
                # Let's go! That's the separator!
                tellStack.append(source.tell())
                return int(digits)
            
            if char == "\n" or char == "":
                raise MFKEOFError("Unexpected EOF!")

            if not char.isdigit(): # It's not a digit.
                raise MFKQueryableError(f"Non-Base10 Digit character '{char}' encountered!", None, char)
            
            # Append string
            digits += char

        raise MFKEOFError("Unexpected EOF!")
    elif val == "?": # A User Input?
        char = source.read(1) # Get the next
        if char == ",": # Is it a comma?
            # Let's go! That's the separator
            tellStack.append(source.tell())
            return InputRequest(None) # Input with no prompt
        
        elif char == "[": # Prompt found!
            promptStream = io.StringIO() # Construct a stream for EZ

            while True: # Parse until end
                promptChar = source.read(1) # Read 1
                if promptChar == "]": # We close
                    break
                elif promptChar == "": # Unexpected EOF
                    raise MFKEOFError("Unexpected EOF while parsing prompt string!")
                promptStream.write(promptChar)

            # Make sure not EOF
            char = source.read(1)
            if char == ",": # We exit
                return InputRequest(promptStream.getvalue())
                
            raise MFKEOFError("Unexpected EOF!")

        elif char == "" or char == "\n": # EOF?
            raise MFKEOFError("Unexpected EOF!")
        
        raise MFKInputRequestError("Unexpected character after input request!")

    raise MFKQueryableError("Queryable is not recognized!", None, None)

def _fetchInitialN(source : io.TextIOBase, START, tellStack : col.deque) -> Queryable:
    letter = source.read(1) # Read the first letter
    if letter != "n": # Not 'n'?
        qual = MFKQueryableError(f"Expected 'n' and got '{letter}' instead!", "n", letter)
        raise MFKParametricError(f"Expected 'n' and got '{letter}' instead!", qual)
    
    assign = source.read(1) # Check for assignment
    if assign == "": # Unexpected EOF?
        raise MFKEOFError("Unexpected EOF when '=' expected!")
    if assign != "=": # Not '=' the assignment?
        qual = MFKQueryableError(f"Expected assignment with '=' and got '{assign}' instead!", "=", assign)
        raise MFKParametricError(f"Expected assignment with '=' and got '{assign}' instead!", qual)
    
    try:
        return _queryableParse(source, START, tellStack)
    except MFKError as e:
        raise MFKParametricError(str(e), e)

def  _fetchMaxIter(source : io.TextIOBase, START, tellStack : col.deque) -> Queryable:
    letter = source.read(1) # Read the first letter
    if letter != "m": # Not 'm'?
        qual = MFKQueryableError(f"Expected 'm' and got '{letter}' instead!", "m", letter)
        raise MFKParametricError(f"Expected 'm' and got '{letter}' instead!", qual)
    
    assign = source.read(1) # Check for assignment
    if assign == "": # Unexpected EOF?
        raise MFKEOFError("Unexpected EOF when '=' expected!")
    if assign != "=": # Not '=' the assignment?
        qual = MFKQueryableError(f"Expected assignment with '=' and got '{assign}' instead!", "=", assign)
        raise MFKParametricError(f"Expected assignment with '=' and got '{assign}' instead!", qual)
    
    # it is darn infinite
    tellStack.append(source.tell())
    discriminator = source.read(1)
    if discriminator == "i": # hmm maybe
        if source.read(1) == "n" and source.read(1) == "f":
            source.seek(tellStack.pop(), io.SEEK_SET)
            return UNBOUNDED
    # otherwise it's a plain Queryable
    source.seek(tellStack.pop(), io.SEEK_SET)
    
    # plain Queryable
    try:
        return _queryableParse(source, START, tellStack)
    except MFKError as e:
        raise MFKParametricError(str(e), e)

@churn.register(io.TextIOBase)
def _(source : io.TextIOBase) -> CompiledMFK:
    """We use streams to help with iteration."""

    src = source # alias for short

    tellStack = col.deque() # Instantiate the position stack
    src.seek(0, io.SEEK_SET)
    START = src.tell() # OK here's the start
    tellStack.append(START)

    # Parse the initial N
    initialN = _fetchInitialN(src, START, tellStack)
    print(initialN)
    
    # parse the max iteration count
    maxIter = _fetchMaxIter(src, START, tellStack)
    print(maxIter)

    return CompiledMFK(initialN=initialN, maxIter=maxIter)


@churn.register(str)
def _(source : str) -> CompiledMFK:
    """Just to help"""
    return churn(io.StringIO(source))



## MAIN

def main():
    import sys, argparse

    parser = argparse.ArgumentParser()
    parser.add_argument('input', 
    nargs='?', 
    type=argparse.FileType('r'), 
    default=sys.stdin,
    help="Input file path (defaults to stdin if omitted). Reads until newline.")

    args = parser.parse_args(sys.argv[1:])

    churn(args.input.readline())

if __name__ == "__main__":
    main()