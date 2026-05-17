import asyncio
import json
import ast
import re
import signal
from typing import Any, Dict, List
from fastapi import FastAPI, WebSocket, WebSocketDisconnect
import uvicorn


# ============================================================
# コード実行サニタイザー
# ============================================================

class CodeSanitizer:
    def __init__(self):
        self.dangerous_imports = {
            'os', 'sys', 'subprocess', 'shutil', 'tempfile', 'glob',
            'socket', 'urllib', 'http', 'ftplib', 'smtplib', 'telnetlib',
            'pickle', 'marshal', 'shelve', 'dill',
            '__import__', 'eval', 'exec', 'compile', 'open', 'file',
            'input', 'raw_input', 'reload', 'vars', 'dir', 'locals', 'globals',
            'exit', 'quit', 'help', 'copyright', 'credits', 'license'
        }

        self.replacements = {
            r'sys\.argv': '"[]"',
            r'\bsys\s*\.\s*argv\b': '"[]"',
            r'\binput\s*\([^)]*\)': '"mocked_input"',
            r'\braw_input\s*\([^)]*\)': '"mocked_input"',
            r'\btime\s*\.\s*sleep\s*\([^)]*\)': 'pass  # sleep disabled',
            r'\bexit\s*\([^)]*\)': 'pass  # exit disabled',
            r'\bquit\s*\([^)]*\)': 'pass  # quit disabled',
            r'\bsys\s*\.\s*exit\s*\([^)]*\)': 'pass  # sys.exit disabled',
        }

    def sanitize_code(self, code: str) -> str:
        sanitized = self.remove_comments_safely(code)
        for pattern, replacement in self.replacements.items():
            sanitized = re.sub(pattern, replacement, sanitized)
        sanitized = self.sanitize_imports(sanitized)
        sanitized = self.sanitize_joins(sanitized)
        return sanitized

    def remove_comments_safely(self, code: str) -> str:
        lines = code.split('\n')
        cleaned_lines = []
        in_function = False

        for line in lines:
            stripped = line.strip()
            if stripped.startswith('def '):
                in_function = True
                cleaned_lines.append(line)
                continue
            if ('"""' in stripped or "'''" in stripped) and in_function:
                cleaned_lines.append(line)
                continue
            cleaned_lines.append(self.remove_comment_from_line(line))
            if in_function and stripped and not line.startswith(' ') and not line.startswith('\t') and not stripped.startswith('def '):
                in_function = False

        return '\n'.join(cleaned_lines)

    def remove_comment_from_line(self, line: str) -> str:
        in_single_quote = False
        in_double_quote = False
        in_triple_single = False
        in_triple_double = False
        escaped = False

        i = 0
        while i < len(line):
            char = line[i]
            if escaped:
                escaped = False
                i += 1
                continue
            if char == '\\':
                escaped = True
                i += 1
                continue
            if i <= len(line) - 3:
                if line[i:i+3] == '"""':
                    if not in_single_quote and not in_triple_single:
                        in_triple_double = not in_triple_double
                        i += 3
                        continue
                elif line[i:i+3] == "'''":
                    if not in_double_quote and not in_triple_double:
                        in_triple_single = not in_triple_single
                        i += 3
                        continue
            if not in_triple_single and not in_triple_double:
                if char == '"' and not in_single_quote:
                    in_double_quote = not in_double_quote
                elif char == "'" and not in_double_quote:
                    in_single_quote = not in_single_quote
            if (char == '#' and
                    not in_single_quote and not in_double_quote and
                    not in_triple_single and not in_triple_double):
                return line[:i].rstrip()
            i += 1
        return line

    def sanitize_imports(self, code: str) -> str:
        lines = code.split('\n')
        sanitized_lines = []
        allowed_modules = ['math', 'random', 'datetime', 'json', 'string']

        for line in lines:
            stripped = line.strip()
            if stripped.startswith('import ') or stripped.startswith('from '):
                is_dangerous = any(d in stripped for d in self.dangerous_imports)
                if is_dangerous:
                    sanitized_lines.append(f"# DISABLED: {line}")
                elif any(f'import {m}' in stripped or f'from {m}' in stripped for m in allowed_modules):
                    sanitized_lines.append(line)
                else:
                    sanitized_lines.append(f"# RESTRICTED: {line}")
            else:
                sanitized_lines.append(line)

        return '\n'.join(sanitized_lines)

    def sanitize_joins(self, code: str) -> str:
        return re.sub(r'\bos\s*\.\s*path\s*\.\s*join\s*\([^)]*\)',
                      '"path_join_disabled"', code)


# ============================================================
# 安全な実行環境
# ============================================================

class SafeExecutionEnvironment:
    def __init__(self):
        self.setup_safe_globals()

    def setup_safe_globals(self):
        self.safe_globals = {
            'len': len, 'range': range, 'enumerate': enumerate,
            'zip': zip, 'map': map, 'filter': filter,
            'sum': sum, 'min': min, 'max': max, 'abs': abs, 'round': round,
            'str': str, 'int': int, 'float': float, 'bool': bool,
            'list': list, 'dict': dict, 'tuple': tuple, 'set': set,
            'type': type, 'isinstance': isinstance,
            'hasattr': hasattr, 'getattr': getattr, 'setattr': setattr,
            'input': lambda prompt="": "mocked_input",
            'print': self.safe_print,
            'Attack': self.attack_function,
            'Guard': self.guard_function,
            'Cast': self.cast_function,
            'MoveTo': self.moveto_function,
            'MoveForward': self.moveforward_function,
            'MoveBackward': self.movebackward_function,
            '__name__': '__main__',
            '__builtins__': {'__name__': '__main__', '__doc__': None},
            'sys': type('MockSys', (), {
                'argv': [], 'version': 'Python (sandboxed)',
                'platform': 'sandboxed', 'exit': lambda *args: None
            })()
        }

    def safe_print(self, *args, **kwargs):
        kwargs.pop('file', None)
        return print(*args, **kwargs)

    def attack_function(self):
        return 'Attacking'

    def guard_function(self):
        return 'Guarding'

    def cast_function(self, spell: str, n: int):
        return f'Casting: {spell}, {n}'

    def moveto_function(self, n: int):
        return f'Moving to: {n}'

    def moveforward_function(self):
        return 'Moving forward'

    def movebackward_function(self):
        return 'Moving backward'


# ============================================================
# コードトレーサー
# ============================================================

class CodeTracer:
    def __init__(self):
        self.logs = []
        self.execution_step = 0
        self.security_step = 0
        self.errors = []
        self.sanitizer = CodeSanitizer()
        self.safe_env = SafeExecutionEnvironment()

    def log(self, msg: str, line: int = 0):
        self.execution_step += 1
        self.logs.append({"t": "execution", "s": self.execution_step, "line": line, "msg": msg})

    def security_log(self, msg: str, line: int = 0):
        self.security_step += 1
        self.logs.append({"t": "security", "s": self.security_step, "line": line, "msg": msg})

    def error(self, msg: str, line: int = 0):
        self.errors.append({"t": "error", "s": self.execution_step, "line": line, "msg": msg})


# ============================================================
# コード実行
# ============================================================

def execute_and_trace(code: str) -> Dict[str, Any]:
    tracer = CodeTracer()

    try:
        if len(code) > 10000:
            tracer.error("Code too long (max 10KB)")
            return _empty_result(tracer)

        original_code = code
        sanitized_code = tracer.sanitizer.sanitize_code(code)
        if original_code != sanitized_code:
            tracer.security_log("Code was sanitized for security")

        try:
            tree = ast.parse(sanitized_code)
        except SyntaxError as e:
            tracer.error(f"Syntax error: {str(e)}")
            return _empty_result(tracer)

        execution_globals = tracer.safe_env.safe_globals.copy()
        execution_locals = {}

        def swap_func(j, k):
            # EnemiesHP の更新（SortBossメカニクス用）
            hp_list = execution_locals.get('EnemiesHP')
            if hp_list is not None:
                if j < 0 or k < 0 or j >= len(hp_list) or k >= len(hp_list):
                    tracer.error(f"Swap: インデックスが範囲外です ({j}, {k})")
                    return f"Swap: {j}, {k}"
                hp_list[j], hp_list[k] = hp_list[k], hp_list[j]

            # numbers の更新（CoreBossメカニクス用）
            numbers_list = execution_locals.get('numbers')
            if isinstance(numbers_list, list):
                if j < 0 or k < 0 or j >= len(numbers_list) or k >= len(numbers_list):
                    tracer.error(f"Swap: インデックスが範囲外です ({j}, {k})")
                    return f"Swap: {j}, {k}"
                numbers_list[j], numbers_list[k] = numbers_list[k], numbers_list[j]

            return f"Swap: {j}, {k}"
        execution_globals['Swap'] = swap_func

        # タイムアウト（Unix系のみ有効）
        timeout_enabled = hasattr(signal, 'SIGALRM')
        if timeout_enabled:
            def timeout_handler(signum, frame):
                raise TimeoutError("Execution timeout")
            signal.signal(signal.SIGALRM, timeout_handler)
            signal.alarm(5)

        try:
            for node in tree.body:
                execute_node(node, tracer, execution_globals, execution_locals)
        except TimeoutError:
            tracer.error("Execution timeout (5s limit)")
        except Exception as e:
            tracer.error(f"Runtime error: {str(e)}")
        finally:
            if timeout_enabled:
                signal.alarm(0)

        # 最終変数を安全にシリアライズ
        import json as _json
        final_result = {}
        for k, v in execution_locals.items():
            if not callable(v) and not k.startswith('_'):
                try:
                    _json.dumps(v, default=str)
                    final_result[k] = v
                except Exception:
                    final_result[k] = str(v)

        execution_logs = [{"s": l["s"], "line": l["line"], "msg": l["msg"]}
                          for l in tracer.logs if l["t"] == "execution"]
        security_logs  = [{"s": l["s"], "line": l["line"], "msg": l["msg"]}
                          for l in tracer.logs if l["t"] == "security"]

        return {
            "executionLogs": execution_logs,
            "securityLogs": security_logs,
            "errors": tracer.errors,
            "finalResult": final_result,
            "sanitized": original_code != sanitized_code,
        }

    except Exception as e:
        tracer.error(f"Unexpected error: {str(e)}")
        return _empty_result(tracer)


def _empty_result(tracer: CodeTracer) -> Dict[str, Any]:
    return {
        "executionLogs": [],
        "securityLogs": [],
        "errors": tracer.errors,
        "finalResult": {},
        "sanitized": False,
    }


def execute_node(node: ast.AST, tracer: CodeTracer, global_vars: dict, local_vars: dict):
    try:
        if isinstance(node, (ast.Import, ast.ImportFrom)):
            tracer.security_log("Import statement detected and controlled", node.lineno)
            return

        if isinstance(node, ast.FunctionDef):
            line_no = node.lineno
            tracer.log(f"Defining function: {node.name}", line_no)
            exec(compile(ast.Module(body=[node], type_ignores=[]), '<string>', 'exec'), global_vars, local_vars)
            tracer.log(f"Function {node.name} defined successfully", line_no)

        elif isinstance(node, ast.Return):
            line_no = node.lineno
            if node.value is not None:
                val = eval(compile(ast.Expression(node.value), '<string>', 'eval'), global_vars, local_vars)
                tracer.log(f"return {ast.unparse(node.value)} → {repr(val)}", line_no)
            else:
                tracer.log("return", line_no)

        elif isinstance(node, ast.Assign):
            line_no = node.lineno
            for target in node.targets:
                if isinstance(target, ast.Name):
                    value = eval(compile(ast.Expression(node.value), '<string>', 'eval'), global_vars, local_vars)
                    if isinstance(value, str) and len(value) > 1000:
                        tracer.security_log("Large string truncated", line_no)
                        value = value[:1000] + "...[truncated]"
                    local_vars[target.id] = value
                    tracer.log(f'{target.id} = {repr(value)}', line_no)

                elif isinstance(target, (ast.Tuple, ast.List)):
                    values = eval(compile(ast.Expression(node.value), '<string>', 'eval'), global_vars, local_vars)
                    if not isinstance(values, (tuple, list)):
                        values = [values]
                    names = []
                    for i, elt in enumerate(target.elts):
                        if isinstance(elt, ast.Name):
                            local_vars[elt.id] = values[i]
                            names.append(f"{elt.id} = {repr(values[i])}")
                        elif isinstance(elt, ast.Subscript):
                            obj_name = elt.value.id if isinstance(elt.value, ast.Name) else str(elt.value)
                            if obj_name in local_vars:
                                idx = eval(compile(ast.Expression(elt.slice), '<string>', 'eval'), global_vars, local_vars)
                                local_vars[obj_name][idx] = values[i]
                                names.append(f"{obj_name}[{idx}] = {repr(values[i])}")
                    tracer.log(f"tuple unpacking: {', '.join(names)}", line_no)

                elif isinstance(target, ast.Subscript):
                    obj_name = target.value.id if isinstance(target.value, ast.Name) else str(target.value)
                    if obj_name in local_vars:
                        obj = local_vars[obj_name]
                        index = eval(compile(ast.Expression(target.slice), '<string>', 'eval'), global_vars, local_vars)
                        if isinstance(obj, (list, tuple)) and (index < 0 or index >= len(obj)):
                            tracer.error(f"Index {index} out of bounds for {obj_name}", line_no)
                            return
                        value = eval(compile(ast.Expression(node.value), '<string>', 'eval'), global_vars, local_vars)
                        obj[index] = value
                        tracer.log(f'{obj_name}[{index}] = {repr(value)} → {obj_name} = {repr(obj)}', line_no)

        elif isinstance(node, ast.AugAssign):
            line_no = node.lineno
            ops = {ast.Add: '+', ast.Sub: '-', ast.Mult: '*', ast.Div: '/'}
            op_sym = ops.get(type(node.op), '?')

            if isinstance(node.target, ast.Subscript):
                obj_name = node.target.value.id if isinstance(node.target.value, ast.Name) else str(node.target.value)
                if obj_name in local_vars:
                    obj = local_vars[obj_name]
                    index = eval(compile(ast.Expression(node.target.slice), '<string>', 'eval'), global_vars, local_vars)
                    right = eval(compile(ast.Expression(node.value), '<string>', 'eval'), global_vars, local_vars)
                    new_val = eval(f"obj[index] {op_sym} right", {'obj': obj, 'index': index, 'right': right})
                    obj[index] = new_val
                    tracer.log(f'{obj_name}[{index}] {op_sym}= {right} → {new_val}', line_no)
            else:
                target_name = node.target.id if isinstance(node.target, ast.Name) else str(node.target)
                if target_name in local_vars:
                    old = local_vars[target_name]
                    right = eval(compile(ast.Expression(node.value), '<string>', 'eval'), global_vars, local_vars)
                    new_val = eval(f"old {op_sym} right", {'old': old, 'right': right})
                    local_vars[target_name] = new_val
                    tracer.log(f'{target_name} {op_sym}= {right} → {new_val}', line_no)
                else:
                    tracer.error(f"Variable '{target_name}' not found for augmented assignment", line_no)

        elif isinstance(node, ast.For):
            target_name = node.target.id
            line_no = node.lineno
            iter_obj = eval(compile(ast.Expression(node.iter), '<string>', 'eval'), global_vars, local_vars)
            iter_list = list(iter_obj)
            if len(iter_list) > 1000:
                tracer.security_log("Loop limited to 1000 iterations", line_no)
                iter_list = iter_list[:1000]
            tracer.log(f'for {target_name} in {ast.unparse(node.iter)} start', line_no)
            for value in iter_list:
                local_vars[target_name] = value
                tracer.log(f'{target_name} = {value}', line_no)
                for stmt in node.body:
                    execute_node(stmt, tracer, global_vars, local_vars)
            tracer.log("for loop end", line_no)

        elif isinstance(node, ast.While):
            loop_count = 0
            max_iterations = 100
            line_no = node.lineno
            tracer.log("while loop start", line_no)
            while True:
                cond = eval(compile(ast.Expression(node.test), '<string>', 'eval'), global_vars, local_vars)
                tracer.log(f"while {ast.unparse(node.test)} → {cond}", line_no)
                if not cond:
                    break
                loop_count += 1
                if loop_count > max_iterations:
                    tracer.security_log(f"While loop stopped after {max_iterations} iterations", line_no)
                    break
                for stmt in node.body:
                    execute_node(stmt, tracer, global_vars, local_vars)
            tracer.log(f"while loop end ({loop_count} iterations)", line_no)

        elif isinstance(node, ast.If):
            line_no = node.lineno
            cond = eval(compile(ast.Expression(node.test), '<string>', 'eval'), global_vars, local_vars)
            tracer.log(f"if {ast.unparse(node.test)} → {cond}", line_no)
            branch = node.body if cond else node.orelse
            for stmt in branch:
                execute_node(stmt, tracer, global_vars, local_vars)

        elif isinstance(node, ast.Expr):
            line_no = node.lineno
            if isinstance(node.value, ast.Call):
                func_name = node.value.func.id if isinstance(node.value.func, ast.Name) else str(node.value.func)
                if func_name in ('eval', 'exec', 'compile', '__import__', 'open', 'file'):
                    tracer.security_log(f"Dangerous function {func_name}() blocked", line_no)
                    return
                if func_name == 'print':
                    if node.value.args:
                        val = eval(compile(ast.Expression(node.value.args[0]), '<string>', 'eval'), global_vars, local_vars)
                        if len(str(val)) > 500:
                            tracer.security_log("Print output truncated", line_no)
                            val = str(val)[:500] + "...[truncated]"
                        tracer.log(f'print({ast.unparse(node.value.args[0])}) → {val}', line_no)
                    else:
                        tracer.log('print() → ', line_no)
                elif func_name in ('Attack', 'Guard', 'Cast', 'MoveTo', 'MoveForward', 'MoveBackward', 'Swap'):
                    result = eval(compile(ast.Expression(node.value), '<string>', 'eval'), global_vars, local_vars)
                    tracer.log(result, line_no)
                else:
                    result = eval(compile(ast.Expression(node.value), '<string>', 'eval'), global_vars, local_vars)
                    if result is not None:
                        tracer.log(f'{ast.unparse(node.value)} → {repr(result)}', line_no)
                    else:
                        tracer.log(f'{ast.unparse(node.value)} executed', line_no)
            else:
                result = eval(compile(ast.Expression(node.value), '<string>', 'eval'), global_vars, local_vars)
                if result is not None:
                    tracer.log(f'{ast.unparse(node.value)} → {repr(result)}', line_no)

        else:
            line_no = getattr(node, 'lineno', 0)
            tracer.log(f"Executing: {type(node).__name__}", line_no)
            exec(compile(ast.Module(body=[node], type_ignores=[]), '<string>', 'exec'), global_vars, local_vars)

    except Exception as e:
        line_no = getattr(node, 'lineno', 0)
        tracer.error(f"Error: {str(e)}", line_no)


# ============================================================
# FastAPI / WebSocket
# ============================================================

app = FastAPI()


@app.websocket("/ws")
async def websocket_endpoint(websocket: WebSocket):
    await websocket.accept()
    print("接続確立")

    try:
        while True:
            data = await websocket.receive_text()
            try:
                request = json.loads(data)
                code = request.get("code", "")
            except json.JSONDecodeError:
                code = data

            if not code.strip():
                await websocket.send_text(json.dumps({
                    "executionLogs": [], "securityLogs": [],
                    "errors": [{"t": "error", "s": 1, "line": 0, "msg": "No code to execute"}],
                    "finalResult": {}
                }))
                continue

            result = execute_and_trace(code)
            await websocket.send_text(json.dumps(result, ensure_ascii=False))
            print(f"実行完了: {len(result['executionLogs'])}ステップ, エラー{len(result['errors'])}件")

    except WebSocketDisconnect:
        print("切断")
    except Exception as e:
        print(f"エラー: {e}")
        try:
            await websocket.send_text(json.dumps({
                "executionLogs": [], "securityLogs": [],
                "errors": [{"t": "error", "s": 1, "line": 0, "msg": f"Server error: {str(e)}"}],
                "finalResult": {}
            }))
        except Exception:
            pass


if __name__ == "__main__":
    print("=" * 50)
    print("  MagiCode ローカルPython実行サーバー")
    print("=" * 50)
    print("  WebSocket: ws://localhost:8000/ws")
    print("  終了: Ctrl+C")
    print("=" * 50)
    uvicorn.run(app, host="127.0.0.1", port=8000)
